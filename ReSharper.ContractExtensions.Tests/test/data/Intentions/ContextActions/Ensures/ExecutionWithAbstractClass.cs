using System.Diagnostics.Contracts;
using System;

[ContractClass(typeof(AContract))]
abstract class A
{
  public abstract strin{caret}g EnabledOnMethodWithContract();
}

[ContractClassFor(typeof(A))]
abstract class AContract : A
{
  public override string EnabledOnMethodWithContract()
  {
    throw new System.NotImplementedException();
  }
}