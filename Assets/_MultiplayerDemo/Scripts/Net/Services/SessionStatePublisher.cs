using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;

namespace Game.Net
{
    /// Общий исполнитель контракта INetSession. Реализация стека сообщает сюда факты,
    /// а правила — что считать отказом и когда рассказывать подписчикам — живут здесь,
    /// один раз, вместо отдельной заплаты в каждом стеке (А-2, А-3, И-10, И-11).
    public sealed class SessionStatePublisher : IDisposable
    {
        private readonly ReactiveProperty<SessionState> _state = new(new SessionState(SessionPhase.Idle));
        private readonly Queue<SessionState> _pending = new();
        private readonly FrameProvider _frames;

        /// Идёт ли попытка или сессия. Вне её отчёты стека ничего не значат: после нашего
        /// собственного выхода стек ещё вправе прислать «отключено», и это не отказ.
        private bool _active;
        private bool _scheduled;
        private bool _closed;

        public ReadOnlyReactiveProperty<SessionState> State => _state;

        public SessionStatePublisher(FrameProvider frames)
        {
            _frames = frames;
        }

        /// Отложенная доставка переживает разбор scope: выход из матча кладёт в очередь Idle, а
        /// следом уходит сцена стека вместе с сессией — и кадром позже работа проснулась бы уже
        /// над закрытым свойством. Поэтому закрытый публикатор молчит, а не бросает.
        public void Dispose()
        {
            _closed = true;
            _pending.Clear();
            _state.Dispose();
        }

        /// Намерение объявляем мы сами — Hosting или Connecting.
        public void Begin(SessionPhase phase)
        {
            _active = true;
            Enqueue(new SessionState(phase));
        }

        /// Отчёт стека, обычно из его коллбэка. Failed и Idle закрывают попытку.
        public void Report(SessionState state)
        {
            if (!_active) return;

            if (state.Phase is SessionPhase.Failed or SessionPhase.Idle) _active = false;
            Enqueue(state);
        }

        /// Наш собственный выход: всё, что стек пришлёт после, отбрасывается.
        public void End()
        {
            _active = false;
            Enqueue(new SessionState(SessionPhase.Idle));
        }

        /// Обёртка старта и подключения: исключение стека — отказ с причиной, отмена — не отказ.
        public async UniTask GuardAsync(Func<UniTask> operation)
        {
            try
            {
                await operation();
            }
            catch (OperationCanceledException)
            {
                End();
            }
            catch (Exception exception)
            {
                Report(new SessionState(SessionPhase.Failed, exception.Message));
            }
        }

        private void Enqueue(SessionState state)
        {
            if (_closed) return;

            _pending.Enqueue(state);

            if (_scheduled) return;

            _scheduled = true;
            _frames.Register(new FlushWork(this));
        }

        private void Flush()
        {
            _scheduled = false;

            if (_closed) return;

            while (_pending.Count > 0)
            {
                _state.Value = _pending.Dequeue();
            }
        }

        /// Работа на один кадр: MoveNext возвращает false, и провайдер кадров её отпускает.
        private sealed class FlushWork : IFrameRunnerWorkItem
        {
            private readonly SessionStatePublisher _owner;

            public FlushWork(SessionStatePublisher owner)
            {
                _owner = owner;
            }

            public bool MoveNext(long frameCount)
            {
                _owner.Flush();
                return false;
            }
        }
    }
}
