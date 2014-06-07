using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using ReSharper.ContractExtensions.ContractsEx;
using ReSharper.ContractExtensions.ContractUtils;
using ReSharper.ContractExtensions.Utilities;

namespace ReSharper.ContractExtensions.ContextActions.Invariants
{
    internal sealed class InvariantActionExecutor
    {
        private readonly InvariantAvailability _invariantAvailability;
        private readonly ICSharpContextActionDataProvider _provider;
        private readonly CSharpElementFactory _factory;
        private readonly ICSharpFile _currentFile;
        private readonly IClassLikeDeclaration _classDeclaration;

        public InvariantActionExecutor(InvariantAvailability invariantAvailability,
            ICSharpContextActionDataProvider provider)
        {
            Contract.Requires(invariantAvailability != null);
            Contract.Requires(invariantAvailability.IsAvailable);
            Contract.Requires(provider != null);

            _invariantAvailability = invariantAvailability;
            _provider = provider;

            _factory = CSharpElementFactory.GetInstance(provider.PsiModule);
            // TODO: look at this class CSharpStatementNavigator

            _classDeclaration = provider.GetSelectedElement<IClassLikeDeclaration>(true, true);
            
            Contract.Assert(provider.SelectedElement != null);
            _currentFile = (ICSharpFile)provider.SelectedElement.GetContainingFile();
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(_invariantAvailability != null);
            Contract.Invariant(_classDeclaration != null);
            Contract.Invariant(_currentFile != null);
            Contract.Invariant(_factory != null);
        }

        public void ExecuteTransaction(ISolution solution, IProgressIndicator progress)
        {
            AddNamespaceUsingIfNecessary();
            var invariantMethod = AddObjectInvariantMethodIfNecessary();

            var invariantStatement = CreateInvariantStatement();
            var addAfter = GetPreviousInvariantStatement();

            AddStatementAfter(invariantMethod, invariantStatement, addAfter);
        }

        private void AddStatementAfter(IMethodDeclaration method, 
            ICSharpStatement statement, ICSharpStatement addAfter)
        {
            method.Body.AddStatementAfter(statement, addAfter);
        }

        /// <summary>
        /// Returns statement after which current invariant should be added.
        /// </summary>
        /// <remarks>
        /// Order of the invariants:
        /// Invariants that check fields
        /// Invariants that check properties
        /// All of them would be in the order of the declaration.
        /// </remarks>
        [CanBeNull]
        private ICSharpStatement GetPreviousInvariantStatement()
        {
            var declaration = _invariantAvailability.FieldOrPropertyDeclaration;

            IEnumerable<IDeclaration> fields = _classDeclaration.MemberDeclarations.OfType<IFieldDeclaration>();
            IEnumerable<IDeclaration> properties = Enumerable.Empty<IDeclaration>();
            if (declaration.IsProperty)
            {
                properties = _classDeclaration.MemberDeclarations.OfType<IPropertyDeclaration>();
            }

            var members = fields.ToList();
            members.AddRange(properties);

            // Creating lookup where key is argument name, and the value is statements.
            var assertions = _classDeclaration.GetInvariantAssertions().ToList();
                //_classDeclaration.GetInvariants()
                    //.SelectMany(x => x.ArgumentNames.Select(a => new { Statement = x, ArgumentName = a }))
                    //.ToLookup(x => x.ArgumentName, x => x.Statement);

            // We should consider only members, declared previously to current member
            var previousInvariants =
                members
                    .Select(p => p.DeclaredName)
                    .TakeWhile(paramName => paramName != _invariantAvailability.SelectedMemberName)
                    .Reverse().ToList();

            // Looking for the last usage of the parameters in the requires statements
            foreach (var p in previousInvariants)
            {
                var assertion = assertions.LastOrDefault(a => a.ChecksForNull(p));
                if (assertion != null)
                {
                    return assertion.Statement;
                }
                //if (requiresStatements.Contains(p))
                //{
                //    return requiresStatements[p].Select(x => x.Statement).LastOrDefault();
                //}
            }

            return null;
        }

        private IMethodDeclaration AddObjectInvariantMethodIfNecessary()
        {
            Contract.Ensures(Contract.Result<IMethodDeclaration>() != null);
            Contract.Ensures(Contract.Result<IMethodDeclaration>().Body != null);

            var invariantMethod = _classDeclaration.GetInvariantMethod();
            if (invariantMethod == null)
                invariantMethod = CreateAndAddObjectInvariantMethod();

            if (!invariantMethod.IsObjectInvariantMethod())
                AddContractInvariantAttribute(invariantMethod);

            return invariantMethod;
        }

        private void AddContractInvariantAttribute(IMethodDeclaration method)
        {
            ITypeElement type = TypeFactory.CreateTypeByCLRName(
                typeof(ContractInvariantMethodAttribute).FullName,
                _provider.PsiModule, _currentFile.GetResolveContext()).GetTypeElement();

            var attribute = _factory.CreateAttribute(type);

            method.AddAttributeBefore(attribute, null);
        }

        [System.Diagnostics.Contracts.Pure]
        private IMethodDeclaration CreateAndAddObjectInvariantMethod()
        {
            Contract.Ensures(Contract.Result<IMethodDeclaration>() != null);

            var method = (IMethodDeclaration)_factory.CreateTypeMemberDeclaration(
                string.Format("private void {0}() {{}}", InvariantUtils.InvariantMethodName),
                EmptyArray<object>.Instance);

            var anchor = GetAnchorForObjectInvariantMethod();

            _classDeclaration.AddClassMemberDeclarationAfter(method, anchor);

            // To enable method modification, method from class declaration should be returned.
            return _classDeclaration.GetInvariantMethod();
        }

        /// <summary>
        /// Returns "acnhor" for contract method: declaration after which ObjectInvariant should be added.
        /// </summary>
        /// <remarks>
        /// ObjectInvariant method should be added after last contructor or after last field declaration.
        /// </remarks>
        [CanBeNull]
        private IClassMemberDeclaration GetAnchorForObjectInvariantMethod()
        {
            var lastConstructor = _classDeclaration.ConstructorDeclarations.LastOrDefault();
            if (lastConstructor != null)
                return lastConstructor;

            return _classDeclaration.MemberDeclarations
                .LastOrDefault(md => md is IFieldDeclaration) as IClassMemberDeclaration;
        }

        private ICSharpStatement CreateInvariantStatement()
        {
            Contract.Ensures(Contract.Result<ICSharpStatement>() != null);

            string stringStatement = string.Format("{0}.Invariant({1} != null);",
                typeof(Contract).Name, _invariantAvailability.SelectedMemberName);
            var statement = _factory.CreateStatement(stringStatement);

            return statement;
        }

        private void AddNamespaceUsingIfNecessary()
        {
            var diagnostics = _factory.CreateUsingDirective("using $0", typeof(Contract).Namespace);
            if (!_currentFile.Imports.ContainsUsing(diagnostics))
            {
                _currentFile.AddImport(diagnostics);
            }
        }
    }
}