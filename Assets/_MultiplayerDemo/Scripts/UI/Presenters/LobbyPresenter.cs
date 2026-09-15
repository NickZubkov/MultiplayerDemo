using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Net;
using R3;
using VContainer.Unity;

namespace Game.UI
{
    /// Только UI лобби: список хостов текущей сети, выбор уровня, кнопки. Сценарий — хост,
    /// подключение, отказы — ушёл в MatchFlow, пауза — в свой презентер (А-6).
    public sealed class LobbyPresenter : IScreen, ILobbyPresenter, IStartable, IDisposable
    {
        /// Широковещание режется брандмауэром и не ходит между сегментами сети, поэтому
        /// пустой список сам по себе ни о чём не говорит — подсказка уводит к ручному вводу.
        private const string EMPTY_HINT = "Хостов не видно. Проверьте, что оба в одной сети, или введите адрес вручную.";

        private const string BAD_MANUAL_ENTRY = "Не удалось разобрать: ";

        private readonly ILobbyView _view;
        private readonly NetworkSlot _slot;
        private readonly IMatchFlow _match;
        private readonly IHudMessages _hud;
        private readonly ArenaDefinition[] _arenas;
        private readonly ReactiveProperty<bool> _wanted = new(false);
        private readonly ReactiveProperty<ArenaDefinition> _selectedArena;

        private DisposableBag _subscriptions;
        private IDisposable _hosts;

        public ScreenLayer Layer => ScreenLayer.Base;
        public bool CapturesInput => true;
        public bool NeedsCursor => true;
        public ReadOnlyReactiveProperty<bool> Wanted => _wanted;

        /// Отмеченный уровень — состояние экрана, а не разделяемое (S4): нужен хосту до старта
        /// и клиенту, подключающемуся вручную (S10).
        public ReadOnlyReactiveProperty<ArenaDefinition> SelectedArena => _selectedArena;

        public LobbyPresenter(ILobbyView view, NetworkSlot slot, IMatchFlow match, IHudMessages hud,
            ArenaDefinition[] arenas)
        {
            _view = view;
            _slot = slot;
            _match = match;
            _hud = hud;
            _arenas = arenas;
            _selectedArena = new ReactiveProperty<ArenaDefinition>(arenas.Length > 0 ? arenas[0] : null);
        }

        public void Start()
        {
            _view.SetEmptyHint(EMPTY_HINT);
            _view.ShowArenas(_arenas);
            _view.MarkArena(_selectedArena.Value);

            _match.Phase.Subscribe(OnPhase).AddTo(ref _subscriptions);
            _view.ArenaChosen.Subscribe(SelectArena).AddTo(ref _subscriptions);
            _view.HostRequested
                .SubscribeAwait((name, token) => _match.HostAsync(name, _selectedArena.Value, token), AwaitOperation.Drop)
                .AddTo(ref _subscriptions);
            _view.JoinRequested
                .SubscribeAwait((entry, token) => _match.JoinAsync(entry, _selectedArena.Value, token), AwaitOperation.Drop)
                .AddTo(ref _subscriptions);
            _view.ManualJoinRequested
                .SubscribeAwait((text, token) => JoinManualAsync(text, token), AwaitOperation.Drop)
                .AddTo(ref _subscriptions);
            _view.BackToStacksRequested
                .SubscribeAwait((_, _) => _match.BackToStacksAsync(), AwaitOperation.Drop)
                .AddTo(ref _subscriptions);
        }

        public void Dispose()
        {
            _hosts?.Dispose();
            _subscriptions.Dispose();
            _wanted.Dispose();
            _selectedArena.Dispose();
        }

        /// Поиск хостов идёт, только пока лобби на экране: в матче он не нужен никому.
        public void Show()
        {
            var stack = _slot.Current.CurrentValue;
            if (stack == null) return;

            _view.SetManualHint(stack.Definition.ManualEntryHint);
            _view.SetJoinVisible(stack.Definition.CanJoin);
            _view.SetHostLabel(stack.Definition.HostButtonLabel);

            _hosts = stack.Directory.Hosts.Subscribe(hosts => _view.ShowHosts(RowsOf(hosts)));
            stack.Directory.StartBrowsing();
            _view.Show();
        }

        public void Hide()
        {
            _hosts?.Dispose();
            _hosts = null;
            _slot.Current.CurrentValue?.Directory.StopBrowsing();
            _view.Hide();
        }

        private void OnPhase(AppPhase phase)
        {
            _wanted.Value = phase is AppPhase.Lobby or AppPhase.Starting;
            _view.SetInteractable(phase == AppPhase.Lobby);
        }

        private void SelectArena(ArenaDefinition arena)
        {
            _selectedArena.Value = arena;
            _view.MarkArena(arena);
        }

        /// Адрес у LAN-стеков и имя сессии у Fusion разбирает сам стек: лобби не знает, что это.
        private async UniTask JoinManualAsync(string text, CancellationToken token)
        {
            var directory = _slot.Current.CurrentValue?.Directory;
            if (directory == null) return;

            if (!directory.TryParseManual(text, out var entry))
            {
                _hud.Show(BAD_MANUAL_ENTRY + text);
                return;
            }

            await _match.JoinAsync(entry, _selectedArena.Value, token);
        }

        /// В строке хоста — его уровень: клиент грузит тот, что выбрал хост, и должен видеть,
        /// куда идёт. Чужой id — это сборка с другим набором уровней.
        private IReadOnlyList<HostRow> RowsOf(IReadOnlyList<HostEntry> hosts)
        {
            var rows = new HostRow[hosts.Count];

            for (var i = 0; i < hosts.Count; i++)
            {
                var host = hosts[i];
                rows[i] = new HostRow(host, $"{host.Name} — {host.Players}/{host.MaxPlayers} — {ArenaName(host)}");
            }

            return rows;
        }

        private string ArenaName(HostEntry host)
        {
            var arenaId = host.MetadataValue(ArenaDefinition.METADATA_KEY);
            if (string.IsNullOrEmpty(arenaId)) return "уровень неизвестен";

            foreach (var arena in _arenas)
            {
                if (arena.ArenaId == arenaId) return arena.DisplayName;
            }

            return "чужой уровень";
        }
    }
}
