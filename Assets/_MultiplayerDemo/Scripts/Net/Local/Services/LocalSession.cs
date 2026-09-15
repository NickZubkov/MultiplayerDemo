using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace Game.Net.Local
{
    /// Одна машина, которая сама себе хост. Мир она наполняет в 15.8; до тех пор старт
    /// хоста — это только смена фазы, и арена стоит пустой.
    public sealed class LocalSession : INetSession, IDisposable
    {
        private const string NOBODY_TO_JOIN = "Без сети подключаться не к кому";

        private static readonly PlayerId ME = new(0);

        private readonly SessionStatePublisher _publisher;
        private readonly PlayerId[] _players = { ME };

        public ReadOnlyReactiveProperty<SessionState> State => _publisher.State;
        public PlayerId LocalPlayer => ME;
        public bool IsJudge => true;
        public IReadOnlyCollection<PlayerId> Players => _players;
        public Observable<PlayerId> PlayerJoined => Observable.Empty<PlayerId>();
        public Observable<PlayerId> PlayerLeft => Observable.Empty<PlayerId>();

        public LocalSession(FrameProvider frames)
        {
            _publisher = new SessionStatePublisher(frames);
        }

        public void Dispose() => _publisher.Dispose();

        public UniTask StartHostAsync(SessionSettings settings, INetWorld world, CancellationToken token)
        {
            _publisher.Begin(SessionPhase.Hosting);
            return UniTask.CompletedTask;
        }

        public UniTask JoinAsync(HostEntry entry, INetWorld world, CancellationToken token)
        {
            _publisher.Begin(SessionPhase.Connecting);
            _publisher.Report(new SessionState(SessionPhase.Failed, NOBODY_TO_JOIN));
            return UniTask.CompletedTask;
        }

        public UniTask LeaveAsync()
        {
            _publisher.End();
            return UniTask.CompletedTask;
        }
    }
}
