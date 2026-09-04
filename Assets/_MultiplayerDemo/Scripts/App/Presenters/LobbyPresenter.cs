using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using R3;
using VContainer.Unity;

namespace Game.App
{
    /// Живёт в scope стека: ему нужны ISessionControl, IHostBrowser и IWorldSpawner.
    /// View приходит из родительского scope сцены Bootstrap, поэтому сам презентер
    /// про конкретный стек ничего не знает — в Mirror и Fusion он тот же самый.
    public sealed class LobbyPresenter : IStartable, IDisposable
    {
        /// Широковещание режется гостевым Wi-Fi и брандмауэром, поэтому пустой список
        /// сам по себе ни о чём не говорит — подсказка уводит к ручному вводу адреса.
        private const string EmptyHint = "Хостов не видно. Проверьте, что оба в одной сети, или введите адрес вручную.";

        private const string UnknownFailure = "Не удалось подключиться";

        private readonly IHostBrowser _browser;
        private readonly ISessionControl _session;
        private readonly IWorldSpawner _spawner;
        private readonly IArenaLoader _arena;
        private readonly IHudMessages _hud;
        private readonly ILobbyView _view;
        private readonly DemoConfig _config;

        private DisposableBag _subscriptions;

        public LobbyPresenter(IHostBrowser browser, ISessionControl session, IWorldSpawner spawner,
            IArenaLoader arena, IHudMessages hud, ILobbyView view, DemoConfig config)
        {
            _browser = browser;
            _session = session;
            _spawner = spawner;
            _arena = arena;
            _hud = hud;
            _view = view;
            _config = config;
        }

        public void Start()
        {
            _view.SetEmptyHint(EmptyHint);

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

            _browser.StartBrowsing();
        }

        /// Браузер здесь не закрываем: он зарегистрирован в том же scope и его сокет
        /// погасит контейнер — иначе Dispose пришёл бы к нему дважды.
        public void Dispose() => _subscriptions.Dispose();

        public async UniTask LeaveAsync()
        {
            await _session.LeaveAsync();
            await _arena.UnloadAsync();
        }

        /// Порядок не косметический: NGO спавнит игрока прямо внутри StartHost, и без
        /// загруженного пола капсула улетает вниз (проверено в задаче 7). Поэтому арена
        /// и точки спавна — до старта сессии, ящики — после: до старта сервера их некуда класть.
        ///
        /// AwaitOperation.Drop у подписки: пока идёт подключение или загрузка сцены,
        /// повторные нажатия игнорируются — в корутинной версии это был бы ручной флаг.
        private async UniTask HostAsync(string playerName, CancellationToken token)
        {
            _spawner.UsePoints(await _arena.LoadAsync(token));
            await _session.StartHostAsync(playerName, token);
            _spawner.SpawnItems();

            /// В LAN о себе рассказывает сам браузер; у облачных стеков этим займётся
            /// их сервис, и приведения просто не случится.
            if (_browser is IHostAdvertiser advertiser)
            {
                advertiser.Advertise(playerName, 1, _config.MaxPlayers);
            }
        }

        /// Клиенту арена нужна не ради точек, а ради пола: игрока ему создаст сервер,
        /// и приземлиться тот должен на уже загруженную сцену.
        private async UniTask JoinAsync(HostEntry entry, CancellationToken token)
        {
            await _arena.LoadAsync(token);
            await _session.JoinAsync(entry, token);
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

        /// Пункт 8 чек-листа: закрытый хост не должен оставить клиента в пустой арене.
        /// Причина отказа уходит в HUD до выгрузки — сообщение переживает смену сцены.
        private async UniTask FailAsync(SessionState state)
        {
            _hud.Show(string.IsNullOrEmpty(state.Reason) ? UnknownFailure : state.Reason);
            await LeaveAsync();
        }
    }
}
