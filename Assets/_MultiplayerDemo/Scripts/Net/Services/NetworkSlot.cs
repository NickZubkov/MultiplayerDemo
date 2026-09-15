using System;
using R3;

namespace Game.Net
{
    /// Гнездо активного стека (спека § 4). Scope стека кладёт сюда себя при сборке и забирает
    /// при разрушении; лобби и сценарий матча видят «текущую сеть» и не знают, какую.
    public sealed class NetworkSlot : IDisposable
    {
        private readonly ReactiveProperty<INetworkStack> _current = new();

        /// Закрытое гнездо молчит, а не бросает. При выходе из Play Mode Unity рушит объекты
        /// в непредсказуемом порядке, и scope стека уходит из гнезда, которое BootstrapScope
        /// успел закрыть раньше. Слушателей к тому моменту нет — сообщать некому.
        private bool _closed;

        public ReadOnlyReactiveProperty<INetworkStack> Current => _current;

        public void Dispose()
        {
            _closed = true;
            _current.Dispose();
        }

        public void Attach(INetworkStack stack)
        {
            if (_closed) return;

            _current.Value = stack;
        }

        /// Уходящий стек не должен выбить из гнезда уже пришедший ему на смену.
        public void Detach(INetworkStack stack)
        {
            if (_closed) return;
            if (_current.Value == stack) _current.Value = null;
        }
    }
}
