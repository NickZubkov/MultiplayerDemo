using NUnit.Framework;

namespace Game.Core.Tests
{
    public sealed class ItemStateTests
    {
        [Test]
        public void StartsFree() => Assert.AreEqual(ItemPhase.Free, new ItemState().Phase);

        [Test]
        public void FirstHoldSucceedsAndRecordsHolder()
        {
            var item = new ItemState();
            Assert.IsTrue(item.TryHold(7));
            Assert.AreEqual(ItemPhase.Held, item.Phase);
            Assert.AreEqual(7ul, item.Holder);
        }

        [Test]
        public void SecondHolderIsRejectedAndFirstKeepsItem()
        {
            var item = new ItemState();
            item.TryHold(7);
            Assert.IsFalse(item.TryHold(9));
            Assert.AreEqual(7ul, item.Holder);
        }

        [Test]
        public void ReleaseByHolderReturnsItemToWorld()
        {
            var item = new ItemState();
            item.TryHold(7);
            Assert.IsTrue(item.TryRelease(7));
            Assert.AreEqual(ItemPhase.Free, item.Phase);
        }

        [Test]
        public void ReleaseByStrangerIsRejected()
        {
            var item = new ItemState();
            item.TryHold(7);
            Assert.IsFalse(item.TryRelease(9));
            Assert.AreEqual(ItemPhase.Held, item.Phase);
        }

        [Test]
        public void RepeatedReleaseIsHarmless()
        {
            var item = new ItemState();
            item.TryHold(7);
            item.TryRelease(7);
            Assert.IsFalse(item.TryRelease(7));
            Assert.AreEqual(ItemPhase.Free, item.Phase);
        }
    }
}
