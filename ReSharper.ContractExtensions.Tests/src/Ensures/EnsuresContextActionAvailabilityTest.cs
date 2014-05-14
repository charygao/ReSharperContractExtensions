﻿using JetBrains.ReSharper.Intentions.CSharp.Test;
using NUnit.Framework;
using ReSharper.ContractExtensions.ContextActions;

namespace ReSharper.ContractExtensions.Tests.Postconditions
{
    [TestFixture]
    public class EnsuresContextActionAvailabilityTest : CSharpContextActionAvailabilityTestBase<EnsuresContextAction>
    {
        protected override string ExtraPath
        {
            get { return "Ensures"; }
        }

        [TestCase("Availability")]
        public void TestSimpleAvailability(string testSrc)
        {
            DoOneTest(testSrc);
        }

        
        [TestCase("AvailabilityFull")]
        [TestCase("AvailabilityOnStaticClass")]
        [Test]
        public void TestOtherAvailability(string testSrc)
        {
            DoOneTest(testSrc);
        }
    }
}