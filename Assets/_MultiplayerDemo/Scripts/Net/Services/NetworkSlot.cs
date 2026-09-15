using System;
using R3;

namespace Game.Net
{
    /// Гнездо активного стека (спека § 4). Scope стека кладёт сюда себя при сборке и забирает
    /// при разрушении; лобби и сценарий матча видят «текущую сеть» и не знают, какую.
    public sealed class NetworkSlot : IDisposable
    {
        private readonly ReactiveProperty<INetworkStack> _current = new();

        public ReadOnlyReactiveProperty<INetworkStack> Current => _current;

        public void Dispose() => _current.Dispose();

        public void Attach(INetworkStack stack) => _current.Value = stack;

        /// Уходящий стек не должен выбить из гнезда уже пришедший ему на смену.
        public void Detach(INetworkStack stack)
        {
            if (_current.Value == stack) _current.Value = null;
        }
    }
}
