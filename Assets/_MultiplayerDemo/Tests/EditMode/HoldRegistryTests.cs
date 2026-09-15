using Game.Core;
using Game.Net;
using Game.Net.Local;
using NUnit.Framework;

namespace Game.Tests
{
    public sealed class HoldRegistryTests
    {
        private static INetEntity Item(string id) => new LocalNetwork().Join().CreateEntity(NetEntityId.Scene(id), PlayerId.NONE);

        [Test]
        public void HolderIsKnownUntilReleased()
        {
            var holds = new HoldRegistry();
            var crate = Item("crate");
            var player = new PlayerId(1);

            holds.Change(crate, PlayerId.NONE, player);
            Assert.IsTrue(holds.TryGetHeldBy(player, out var held));
            Assert.AreSame(crate, held);

            holds.Change(crate, player, PlayerId.NONE);
            Assert.IsFalse(holds.IsHolding(player));
        }

        /// Опоздавшее «отпустил» от одного ящика не должно освободить руки, которые уже
        /// держат другой.
        [Test]
        public void ReleaseOfAnotherItemKeepsHands()
        {
            var holds = new HoldRegistry();
            var first = Item("first");
            var second = Item("second");
            var player = new PlayerId(1);

            holds.Change(second, PlayerId.NONE, player);
            holds.Change(first, player, PlayerId.NONE);

            Assert.IsTrue(holds.IsHolding(player));
        }
    }
}
