using System;
using VContainer.Unity;

namespace Game.Net
{
    /// Точка входа scope стека: стек встаёт в гнездо при старте и уходит при разрушении
    /// сцены. Одна на все четыре стека — у всех одна и та же пара действий.
    public sealed class StackAttachment : IStartable, IDisposable
    {
        private readonly NetworkSlot _slot;
        private readonly INetworkStack _stack;

        public StackAttachment(NetworkSlot slot, INetworkStack stack)
        {
            _slot = slot;
            _stack = stack;
        }

        public void Start() => _slot.Attach(_stack);

        public void Dispose() => _slot.Detach(_stack);
    }
}
