using System;
using R3;

namespace Game.Net
{
    /// Сторона игры (спека § 5.5). Команда исполняется у авторитета, состояние расходится
    /// оттуда же. OnState отдаёт текущее состояние сразу — опоздавшему клиенту тоже.
    public interface INetEntity
    {
        public NetEntityId Id { get; }
        public PlayerId Owner { get; }
        public bool IsAuthority { get; }
        public Observable<Unit> AuthorityChanged { get; }

        public void SetState<TState>(in TState state) where TState : struct, INetMessage;
        public IDisposable OnState<TState>(Action<TState> handler) where TState : struct, INetMessage;
        public IDisposable On<TMessage>(Action<PlayerId, TMessage> handler) where TMessage : struct, INetMessage;
        public void Send<TCommand>(in TCommand command) where TCommand : struct, INetMessage;
        public void Notify<TEvent>(PlayerId target, in TEvent message) where TEvent : struct, INetMessage;
        public void Broadcast<TEvent>(in TEvent message) where TEvent : struct, INetMessage;
        public void Snap();
    }
}
