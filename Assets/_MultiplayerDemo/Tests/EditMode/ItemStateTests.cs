using Game.Core;
using Game.Net;
using NUnit.Framework;

namespace Game.Tests
{
    public sealed class ItemStateTests
    {
        [Test]
        public void StartsFree() => Assert.AreEqual(ItemPhase.Free, new ItemState().Phase);

        [Test]
        public void FirstHoldSucceedsAndRecordsHolder()
        {
            var item = new ItemState();
            Assert.IsTrue(item.TryHold(new PlayerId(7)));
            Assert.AreEqual(ItemPhase.Held, item.Phase);
            Assert.AreEqual(new PlayerId(7), item.Holder);
        }

        [Test]
        public void SecondHolderIsRejectedAndFirstKeepsItem()
        {
            var item = new ItemState();
            item.TryHold(new PlayerId(7));
            Assert.IsFalse(item.TryHold(new PlayerId(9)));
            Assert.AreEqual(new PlayerId(7), item.Holder);
        }

        [Test]
        public void ReleaseByHolderReturnsItemToWorld()
        {
            var item = new ItemState();
            item.TryHold(new PlayerId(7));
            Assert.IsTrue(item.TryRelease(new PlayerId(7)));
            Assert.AreEqual(ItemPhase.Free, item.Phase);
        }

        [Test]
        public void ReleaseByStrangerIsRejected()
        {
            var item = new ItemState();
            item.TryHold(new PlayerId(7));
            Assert.IsFalse(item.TryRelease(new PlayerId(9)));
            Assert.AreEqual(ItemPhase.Held, item.Phase);
        }

        [Test]
        public void RepeatedReleaseIsHarmless()
        {
            var item = new ItemState();
            item.TryHold(new PlayerId(7));
            item.TryRelease(new PlayerId(7));
            Assert.IsFalse(item.TryRelease(new PlayerId(7)));
            Assert.AreEqual(ItemPhase.Free, item.Phase);
        }

        /// Смена судьи у Fusion: новый авторитет собирает автомат из пришедшего состояния.
        [Test]
        public void RestoreRebuildsFromState()
        {
            var item = new ItemState();

            item.Restore(new PlayerId(4));
            Assert.AreEqual(ItemPhase.Held, item.Phase);
            Assert.IsFalse(item.TryHold(new PlayerId(5)));

            item.Restore(PlayerId.NONE);
            Assert.AreEqual(ItemPhase.Free, item.Phase);
        }

        [Test]
        public void NobodyCannotHold() => Assert.IsFalse(new ItemState().TryHold(PlayerId.NONE));
    }
}
