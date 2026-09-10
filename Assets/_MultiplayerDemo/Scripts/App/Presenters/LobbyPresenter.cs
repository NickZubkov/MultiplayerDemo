using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using R3;
using VContainer.Unity;

namespace Game.App
{
    /// Живёт в scope стека: ему нужны ISessionControl, IHostBrowser и IWorldSpawner.
    /// Виды приходят из родительского scope сцены Bootstrap, поэтому сам презентер
    /// про конкретный стек ничего не знает — в Mirror и Fusion он тот же самый.
    public sealed class LobbyPresenter : IStartable, IDisposable
    {
        /// Широковещание режется брандмауэром и не ходит между сегментами сети, поэтому
        /// пустой список сам по себе ни о чём не говорит — подсказка уводит к ручному вводу.
        private const string EmptyHint = "Хостов не видно. Проверьте, что оба в одной сети, или введите адрес вручную.";

        private const string UnknownFailure = "Не удалось подключиться";

        private const string UnknownArena = "Хост играет на уровне, которого нет в этой сборке";

        private readonly IHostBrowser _browser;
        private readonly ISessionControl _session;
        private readonly IWorldSpawner _spawner;
        private readonly IArenaLoader _arena;
        private readonly IStackFlow _stackFlow;
        private readonly IHudMessages _hud;
        private readonly ILobbyView _view;
        private readonly IPauseView _pause;
        private readonly DemoConfig _config;
        private readonly NetworkStackDefinition _stack;
        private readonly ArenaDefinition[] _arenas;

        private DisposableBag _subscriptions;

        /// Отмеченный уровень — состояние экрана, а не разделяемое состояние: он нужен
        /// только хосту и только до старта. Клиент берёт уровень из строки хоста.
        private ArenaDefinition _selectedArena;

        private bool _paused;

        private bool InMatch =>
            _session.State.CurrentValue.Phase is SessionPhase.Hosting or SessionPhase.Connected;

        public LobbyPresenter(IHostBrowser browser, ISessionControl session, IWorldSpawner spawner,
            IArenaLoader arena, IStackFlow stackFlow, IHudMessages hud, ILobbyView view, IPauseView pause,
            DemoConfig config, NetworkStackDefinition stack, ArenaDefinition[] arenas)
        {
            _browser = browser;
            _session = session;
            _spawner = spawner;
            _arena = arena;
            _stackFlow = stackFlow;
            _hud = hud;
            _view = view;
            _pause = pause;
            _config = config;
            _stack = stack;
            _arenas = arenas;
            _selectedArena = arenas.Length > 0 ? arenas[0] : null;
        }

        public void Start()
        {
            _view.SetEmptyHint(EmptyHint);
            _view.SetManualHint(_stack.ManualEntryHint);
            _view.ShowArenas(_arenas);
            _view.MarkArena(_selectedArena);

            _browser.Hosts
                    .Subscribe(_view.ShowHosts)
                    .AddTo(ref _subscriptions);

            /// Подписка отдаёт текущее значение сразу, поэтому отдельный показ
            /// лобби на старте не нужен: сцена стека только что загрузилась, фаза Idle.
            _session.State
                    .Subscribe(state => ShowLobbyOutOfGame(state.Phase))
                    .AddTo(ref _subscriptions);

            _session.State
                    .Where(state => state.Phase == SessionPhase.Failed)
                    .SubscribeAwait((state, _) => FailAsync(state), AwaitOperation.Drop)
                    .AddTo(ref _subscriptions);

            _view.HostRequested
                 .SubscribeAwait((name, token) => HostAsync(name, token), AwaitOperation.Drop)
                 .AddTo(ref _subscriptions);

            _view.JoinRequested
                 .SubscribeAwait((entry, token) => JoinAsync(entry, token), AwaitOperation.Drop)
                 .AddTo(ref _subscriptions);

            _view.ArenaChosen
                 .Subscribe(SelectArena)
                 .AddTo(ref _subscriptions);

            _view.BackToStacksRequested
                 .SubscribeAwait((_, _) => BackToStacksAsync(), AwaitOperation.Drop)
                 .AddTo(ref _subscriptions);

            _pause.ToggleRequested
                  .Subscribe(_ => TogglePause())
                  .AddTo(ref _subscriptions);

            _pause.ResumeRequested
                  .Subscribe(_ => ClosePause(true))
                  .AddTo(ref _subscriptions);

            _pause.ExitRequested
                  .SubscribeAwait((_, _) => ExitToLobbyAsync(), AwaitOperation.Drop)
                  .AddTo(ref _subscriptions);

            _browser.StartBrowsing();
        }

        /// Браузер здесь не закрываем: он зарегистрирован в том же scope и его сокет
        /// погасит контейнер — иначе Dispose пришёл бы к нему дважды.
        ///
        /// А вот экраны погасить обязаны: они живут в scope сцены Bootstrap и переживают
        /// этот презентер. Со сценой стека уходит он, а панель лобби остаётся висеть
        /// поверх выбора стека — она ниже по иерархии, значит рисуется сверху и
        /// перехватывает клики по кнопкам стеков. Кто показал, тот и прячет.
        public void Dispose()
        {
            _subscriptions.Dispose();
            _view.Hide();

            /// Курсор не захватываем: впереди выбор стека, там он нужен игроку.
            _pause.Hide(false);
        }

        public async UniTask LeaveAsync()
        {
            await _session.LeaveAsync();
            await _arena.UnloadAsync();
        }

        /// Порядок не косметический: NGO спавнит игрока прямо внутри StartHost, и без
        /// загруженного пола капсула улетает вниз (проверено в задаче 7). Поэтому арена —
        /// до старта сессии, ящики — после: до старта сервера их некуда класть. Точки
        /// спавнер получает сам, от ArenaScope, в момент загрузки сцены.
        ///
        /// AwaitOperation.Drop у подписки: пока идёт подключение или загрузка сцены,
        /// повторные нажатия игнорируются — в корутинной версии это был бы ручной флаг.
        private async UniTask HostAsync(string playerName, CancellationToken token)
        {
            await _arena.LoadAsync(_selectedArena, token);
            await _session.StartHostAsync(playerName, _selectedArena.ArenaId, token);
            _spawner.SpawnItems();

            /// В LAN о себе рассказывает сам браузер; у облачных стеков этим занимается
            /// сессия — уровень она объявила при создании комнаты, и приведения
            /// просто не случится.
            if (_browser is IHostAdvertiser advertiser)
            {
                advertiser.Advertise(playerName, 1, _config.MaxPlayers, _selectedArena.ArenaId);
            }
        }

        /// Клиенту арена нужна не ради точек, а ради пола: игрока ему создаст сервер,
        /// и приземлиться тот должен на уже загруженную сцену — и ровно на ту же, что у хоста.
        private async UniTask JoinAsync(HostEntry entry, CancellationToken token)
        {
            var arena = ArenaOf(entry);
            if (arena == null)
            {
                _hud.Show(UnknownArena);
                return;
            }

            await _arena.LoadAsync(arena, token);
            await _session.JoinAsync(entry, token);
        }

        /// Пустой ArenaId бывает только у записи, собранной из введённого вручную адреса:
        /// маяка не было, спросить некого — идём на свой отмеченный уровень и надеемся,
        /// что собеседник сделал так же (решение S10 в спеке потока сцен).
        private ArenaDefinition ArenaOf(HostEntry entry)
        {
            if (string.IsNullOrEmpty(entry.ArenaId)) return _selectedArena;

            foreach (var arena in _arenas)
            {
                if (arena.ArenaId == entry.ArenaId) return arena;
            }

            return null;
        }

        private void SelectArena(ArenaDefinition arena)
        {
            _selectedArena = arena;
            _view.MarkArena(arena);
        }

        /// Панель лобби в сцене Bootstrap выключена и включается отсюда: в арене она
        /// перекрывала бы обзор, а после отказа обязана вернуться — иначе игрок
        /// останется смотреть на пустую сцену без единой кнопки.
        private void ShowLobbyOutOfGame(SessionPhase phase)
        {
            if (phase is SessionPhase.Hosting or SessionPhase.Connected)
            {
                _view.Hide();
            }
            else
            {
                _view.Show();
            }
        }

        /// Вне матча пауза бессмысленна: в лобби курсор и так свободен, а выходить неоткуда.
        private void TogglePause()
        {
            if (!InMatch) return;

            if (_paused)
            {
                ClosePause(true);
                return;
            }

            _paused = true;
            _pause.Show();
        }

        private void ClosePause(bool captureCursor)
        {
            if (!_paused) return;

            _paused = false;
            _pause.Hide(captureCursor);
        }

        /// Курсор в захват не возвращаем: впереди лобби, там он нужен игроку.
        private async UniTask ExitToLobbyAsync()
        {
            ClosePause(false);
            await LeaveAsync();
        }

        /// Сцену стека выгружает StackFlow из scope лобби: этот презентер живёт в ней же
        /// и до конца операции не дожил бы. Сессию и арену закрываем до просьбы —
        /// рвать надо снизу вверх.
        private async UniTask BackToStacksAsync()
        {
            await LeaveAsync();
            _stackFlow.RequestBackToSelect();
        }

        /// Пункт 8 чек-листа: закрытый хост не должен оставить клиента в пустой арене.
        /// Причина отказа уходит в HUD до выгрузки — сообщение переживает смену сцены.
        private async UniTask FailAsync(SessionState state)
        {
            _hud.Show(string.IsNullOrEmpty(state.Reason) ? UnknownFailure : state.Reason);
            ClosePause(false);
            await LeaveAsync();
        }
    }
}
