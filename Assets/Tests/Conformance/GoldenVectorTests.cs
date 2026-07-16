using NUnit.Framework;
using NetNinja.Core.State;
using NetNinja.Core;

namespace NetNinja.Conformance.Tests
{
    public class GoldenVectorTests
    {
        [Test]
        public void ConfigHash_MatchesOracle()
        {
            Assert.AreEqual("6c3a8288f02919a3", FnvStateHasher.HashConfig(CoreConfig.CreateDefault()));
        }
    }
}
