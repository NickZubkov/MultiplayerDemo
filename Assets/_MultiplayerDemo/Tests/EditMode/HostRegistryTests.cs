using Game.Core;
using Game.Net;
using NUnit.Framework;

namespace Game.Tests
{
    public sealed class HostRegistryTests
    {
        private static HostEntry Host(string token, int players = 1) => new HostEntry("Коля", players, 4, "ngo", token, "box");

        [Test]
        public void ReportedHostBecomesVisible()
        {
            var registry = new HostRegistry();
            registry.Report(Host("192.168.0.2:7777"), 0);
            Assert.AreEqual(1, registry.GetAlive(0).Count);
        }

        [Test]
        public void SameTokenUpdatesInsteadOfDuplicating()
        {
            var registry = new HostRegistry();
            registry.Report(Host("192.168.0.2:7777", 1), 0);
            registry.Report(Host("192.168.0.2:7777", 2), 1);
            var alive = registry.GetAlive(1);
            Assert.AreEqual(1, alive.Count);
            Assert.AreEqual(2, alive[0].Players);
        }

        [Test]
        public void DifferentTokensCoexist()
        {
            var registry = new HostRegistry();
            registry.Report(Host("192.168.0.2:7777"), 0);
            registry.Report(Host("192.168.0.3:7777"), 0);
            Assert.AreEqual(2, registry.GetAlive(0).Count);
        }

        [Test]
        public void HostDisappearsAfterTtl()
        {
            var registry = new HostRegistry();
            registry.Report(Host("192.168.0.2:7777"), 0);
            Assert.AreEqual(0, registry.GetAlive(HostRegistry.TTL + 0.01).Count);
        }

        [Test]
        public void HostSurvivesRightBeforeTtl()
        {
            var registry = new HostRegistry();
            registry.Report(Host("192.168.0.2:7777"), 0);
            Assert.AreEqual(1, registry.GetAlive(HostRegistry.TTL - 0.01).Count);
        }

        [Test]
        public void RefreshExtendsLifetime()
        {
            var registry = new HostRegistry();
            registry.Report(Host("192.168.0.2:7777"), 0);
            registry.Report(Host("192.168.0.2:7777"), 2);
            Assert.AreEqual(1, registry.GetAlive(4).Count);
        }
    }
}
