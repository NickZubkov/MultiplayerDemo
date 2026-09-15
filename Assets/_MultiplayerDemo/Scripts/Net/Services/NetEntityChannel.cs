using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Game.Net
{
    /// Общий для всех стеков перевод между типизированными сообщениями игры и байтами
    /// носителя. Стек пишет только носитель, раздачу по типам — никто, кроме этого класса.
    public sealed class NetEntityChannel : INetEntity, IDisposable
    {
        private readonly INetCarrier _carrier;
        private readonly Dictionary<uint, Action<PlayerId, NetBlob>> _handlers = new();
        private readonly Subject<NetBlob> _states = new();
        private readonly Subject<Unit> _authorityChanged = new();

        private NetBlob? _lastState;

        public NetEntityId Id => _carrier.Id;
        public PlayerId Owner => _carrier.Owner;
        public bool IsAuthority => _carrier.IsAuthority;
        public Observable<Unit> AuthorityChanged => _authorityChanged;

        public NetEntityChannel(INetCarrier carrier)
        {
            _carrier = carrier;
        }

        public void Dispose()
        {
            _states.Dispose();
            _authorityChanged.Dispose();
            _handlers.Clear();
        }

        /// Своим обработчикам авторитет раздаёт состояние сразу: логика одинакова на всех
        /// машинах и не должна различать «я записал» и «мне прислали».
        public void SetState<TState>(in TState state) where TState : struct, INetMessage
        {
            if (!IsAuthority) throw new InvalidOperationException($"Сущность {Id}: состояние пишет только авторитет");

            var blob = Encode(state);
            _carrier.PublishState(blob);
            ReceiveState(blob);
        }

        public IDisposable OnState<TState>(Action<TState> handler) where TState : struct, INetMessage
        {
            if (_lastState.HasValue) handler(Decode<TState>(_lastState.Value));

            return _states.Subscribe(blob => handler(Decode<TState>(blob)));
        }

        /// Один обработчик на тип: два подписчика одной команды — это две логики на одной
        /// сущности, то есть ошибка сборки префаба, и о ней надо узнать сразу.
        public IDisposable On<TMessage>(Action<PlayerId, TMessage> handler) where TMessage : struct, INetMessage
        {
            var type = MessageId<TMessage>.VALUE;

            if (_handlers.ContainsKey(type))
            {
                throw new InvalidOperationException($"Сущность {Id}: второй обработчик {typeof(TMessage).Name}");
            }

            _handlers[type] = (sender, blob) => handler(sender, Decode<TMessage>(blob));
            return Disposable.Create(() => _handlers.Remove(type));
        }

        public void Send<TCommand>(in TCommand command) where TCommand : struct, INetMessage =>
            _carrier.SendToAuthority(MessageId<TCommand>.VALUE, Encode(command));

        public void Notify<TEvent>(PlayerId target, in TEvent message) where TEvent : struct, INetMessage =>
            _carrier.SendToPlayer(target, MessageId<TEvent>.VALUE, Encode(message));

        public void Broadcast<TEvent>(in TEvent message) where TEvent : struct, INetMessage =>
            _carrier.SendToAll(MessageId<TEvent>.VALUE, Encode(message));

        public void Snap() => _carrier.Snap();

        public void ReceiveState(in NetBlob state)
        {
            _lastState = state;
            _states.OnNext(state);
        }

        /// Сообщение без обработчика — ошибка сборки, а не норма: молча его не глотаем.
        public void Receive(PlayerId sender, uint type, in NetBlob payload)
        {
            if (_handlers.TryGetValue(type, out var handler))
            {
                handler(sender, payload);
                return;
            }

            Debug.LogWarning($"Сущность {Id}: нет обработчика сообщения {type:X8} от {sender}");
        }

        public void RaiseAuthorityChanged() => _authorityChanged.OnNext(Unit.Default);

        private static NetBlob Encode<T>(in T message) where T : struct, INetMessage
        {
            Span<byte> buffer = stackalloc byte[NetBlob.CAPACITY];
            var writer = new NetWriter(buffer);
            var copy = message;
            copy.Write(ref writer);
            return NetBlob.From(writer.Written);
        }

        private static T Decode<T>(in NetBlob blob) where T : struct, INetMessage
        {
            Span<byte> buffer = stackalloc byte[NetBlob.CAPACITY];
            var length = blob.CopyTo(buffer);
            var reader = new NetReader(buffer.Slice(0, length));
            var message = default(T);
            message.Read(ref reader);
            return message;
        }
    }
}
