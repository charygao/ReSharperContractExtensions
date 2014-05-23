﻿using JetBrains.ReSharper.Intentions.CSharp.Test;
using NUnit.Framework;
using ReSharper.ContractExtensions.ContextActions;
using ReSharper.ContractExtensions.ContextActions.Requires;

namespace ReSharper.ContractExtensions.Tests.Preconditions
{
    [TestFixture]
    public class RequiresContextActionAvailabilityTest : CSharpContextActionAvailabilityTestBase<RequiresContextAction>
    {
        protected override string ExtraPath
        {
            get { return "Requires"; }
        }

        [TestCase("Availability")]
        [Test]
        public void TestSimpleAvailability(string testSrc)
        {
            DoOneTest(testSrc);
        }

        [TestCase("AvailabilityFull")]
        [TestCase("AvailabilityOnAbstractClass")]
        [TestCase("AvailabilityOnInterface")]
        [TestCase("AvailabilityOnStaticClass")]
        [Test]
        public void TestFullAvailability(string testSrc)
        {
            DoOneTest(testSrc);
        }
    }
}