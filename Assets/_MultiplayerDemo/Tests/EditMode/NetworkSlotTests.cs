using Game.Net;
using NUnit.Framework;

namespace Game.Tests
{
    public sealed class NetworkSlotTests
    {
        /// Гнезду достаточно ссылки на стек: ни сессия, ни каталог здесь не зовутся.
        private sealed class FakeStack : INetworkStack
        {
            public NetworkStackDefinition Definition => null;
            public INetSession Session => null;
            public IHostDirectory Directory => null;
        }

        [Test]
        public void AttachedStackIsCurrent()
        {
            using var slot = new NetworkSlot();
            var stack = new FakeStack();

            slot.Attach(stack);

            Assert.AreSame(stack, slot.Current.CurrentValue);
        }

        /// Сцена нового стека успевает встать в гнездо раньше, чем выгрузится старая:
        /// уходящий обязан забрать только себя.
        [Test]
        public void DetachOfOtherStackIsIgnored()
        {
            using var slot = new NetworkSlot();
            var leaving = new FakeStack();
            var arriving = new FakeStack();
            slot.Attach(leaving);
            slot.Attach(arriving);

            slot.Detach(leaving);

            Assert.AreSame(arriving, slot.Current.CurrentValue);
        }

        /// При выходе из Play Mode BootstrapScope может закрыться раньше scope стека,
        /// и тот уходит из уже закрытого гнезда. Закрытое гнездо молчит, а не бросает.
        [Test]
        public void ClosedSlotIgnoresLateStackMoves()
        {
            var slot = new NetworkSlot();
            var stack = new FakeStack();
            slot.Attach(stack);
            slot.Dispose();

            Assert.DoesNotThrow(() => slot.Detach(stack));
            Assert.DoesNotThrow(() => slot.Attach(stack));
        }
    }
}
