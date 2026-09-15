using Game.Gameplay;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// Токены, а не флаг: два модальных экрана подряд не должны отпускать ввод друг за друга.
    public sealed class InputGateTests
    {
        [Test]
        public void OpenByDefault() => Assert.IsFalse(new InputGate().IsBlocked);

        [Test]
        public void BlockClosesUntilDisposed()
        {
            var gate = new InputGate();

            var block = gate.Block();
            Assert.IsTrue(gate.IsBlocked);

            block.Dispose();
            Assert.IsFalse(gate.IsBlocked);
        }

        [Test]
        public void TwoBlocksNeedTwoReleases()
        {
            var gate = new InputGate();
            var first = gate.Block();
            var second = gate.Block();

            first.Dispose();
            Assert.IsTrue(gate.IsBlocked);

            second.Dispose();
            Assert.IsFalse(gate.IsBlocked);
        }

        [Test]
        public void RepeatedDisposeIsHarmless()
        {
            var gate = new InputGate();
            var first = gate.Block();
            var second = gate.Block();

            first.Dispose();
            first.Dispose();

            Assert.IsTrue(gate.IsBlocked);
            second.Dispose();
        }
    }
}
