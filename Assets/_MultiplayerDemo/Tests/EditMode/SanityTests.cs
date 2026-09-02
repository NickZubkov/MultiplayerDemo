using NUnit.Framework;

namespace Game.Core.Tests
{
    public sealed class SanityTests
    {
        [Test]
        public void TestRunnerSeesThisAssembly() => Assert.AreEqual(4, 2 + 2);
    }
}
