using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Net;
using R3;
using VContainer.Unity;

namespace Game.App
{
    /// Сценарий матча (спека § 8.1). Живёт в BootstrapScope, выше всех сцен, которые грузит
    /// и выгружает, — поэтому его продолжения после await не просыпаются в мёртвом объекте,
    /// и обходной путь через подписку в StackFlow больше не нужен.
    public sealed class MatchFlow : IMatchFlow, IStartable, IDisposable
    {
        private const string UNKNOWN_ARENA = "Хост играет на уровне, которого нет в этой сборке";
        private const string UNKNOWN_FAILURE = "Не удалось подключиться";

        private readonly NetworkSlot _slot;
        private readonly IStackFlow _stacks;
        private readonly IArenaLoader _arena;
        private readonly DemoConfig _config;
        private readonly ArenaDefinition[] _arenas;
        private readonly ReactiveProperty<AppPhase> _phase = new(AppPhase.SelectingStack);
        private readonly Subject<string> _failures = new();

        private DisposableBag _subscriptions;
        private IDisposable _session;

        public ReadOnlyReactiveProperty<AppPhase> Phase => _phase;
        public Observable<string> Failures => _failures;

        private INetSession Session => _slot.Current.CurrentValue?.Session;

        public MatchFlow(NetworkSlot slot, IStackFlow stacks, IArenaLoader arena, DemoConfig config,
            ArenaDefinition[] arenas)
        {
            _slot = slot;
            _stacks = stacks;
            _arena = arena;
            _config = config;
            _arenas = arenas;
        }

        public void Start() => _slot.Current.Subscribe(OnStackChanged).AddTo(ref _subscriptions);

        public void Dispose()
        {
            _session?.Dispose();
            _subscriptions.Dispose();
            _phase.Dispose();
            _failures.Dispose();
        }

        public async UniTask HostAsync(string playerName, ArenaDefinition arena, CancellationToken token)
        {
            var session = Session;
            if (session == null || _phase.Value != AppPhase.Lobby) return;

            var metadata = new Dictionary<string, string> { [ArenaDefinition.METADATA_KEY] = arena.ArenaId };
            var settings = new SessionSettings(playerName, _config.MaxPlayers, metadata);

            await StartAsync(arena, token, world => session.StartHostAsync(settings, world, token));
        }

        public async UniTask JoinAsync(HostEntry entry, ArenaDefinition fallback, CancellationToken token)
        {
            var session = Session;
            if (session == null || _phase.Value != AppPhase.Lobby) return;

            var arena = ArenaOf(entry, fallback);
            if (arena == null)
            {
                _failures.OnNext(UNKNOWN_ARENA);
                return;
            }

            await StartAsync(arena, token, world => session.JoinAsync(entry, world, token));
        }

        public async UniTask LeaveAsync()
        {
            if (Session == null) return;

            await TearDownAsync();
            _phase.Value = AppPhase.Lobby;
        }

        /// Фаза SelectingStack наступит сама — когда scope стека уйдёт из гнезда.
        public async UniTask BackToStacksAsync()
        {
            if (Session != null) await TearDownAsync();

            await _stacks.UnloadAsync();
        }

        /// Арена — до сессии (S3). Отменённая загрузка прибирает за собой: сцена догрузится
        /// всё равно, и выгрузить её обязаны (И-12).
        private async UniTask StartAsync(ArenaDefinition arena, CancellationToken token, Func<INetWorld, UniTask> start)
        {
            _phase.Value = AppPhase.Starting;

            try
            {
                var world = await _arena.LoadAsync(arena, token);
                await start(world);
            }
            catch (OperationCanceledException)
            {
                await _arena.UnloadAsync();
                _phase.Value = AppPhase.Lobby;
            }
        }

        /// Рвём снизу вверх: сессия, потом арена.
        private async UniTask TearDownAsync()
        {
            _phase.Value = AppPhase.Leaving;
            await Session.LeaveAsync();
            await _arena.UnloadAsync();
        }

        private void OnStackChanged(INetworkStack stack)
        {
            _session?.Dispose();
            _session = null;

            if (stack == null)
            {
                _phase.Value = AppPhase.SelectingStack;
                return;
            }

            _phase.Value = AppPhase.Lobby;
            _session = Disposable.Combine(
                stack.Session.State
                    .Where(state => state.Phase is SessionPhase.Hosting or SessionPhase.Connected)
                    .Subscribe(_ => _phase.Value = AppPhase.InMatch),
                stack.Session.State
                    .Where(state => state.Phase == SessionPhase.Failed)
                    .SubscribeAwait((state, _) => FailAsync(state), AwaitOperation.Drop));
        }

        /// Причина уходит в Failures до выгрузки: сообщение переживает смену сцены.
        private async UniTask FailAsync(SessionState state)
        {
            _failures.OnNext(string.IsNullOrEmpty(state.Reason) ? UNKNOWN_FAILURE : state.Reason);
            await TearDownAsync();
            _phase.Value = AppPhase.Lobby;
        }

        private ArenaDefinition ArenaOf(HostEntry entry, ArenaDefinition fallback)
        {
            var arenaId = entry.MetadataValue(ArenaDefinition.METADATA_KEY);
            if (string.IsNullOrEmpty(arenaId)) return fallback;

            foreach (var arena in _arenas)
            {
                if (arena.ArenaId == arenaId) return arena;
            }

            return null;
        }
    }
}
