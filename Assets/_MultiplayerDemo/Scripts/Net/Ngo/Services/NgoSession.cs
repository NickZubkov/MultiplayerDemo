using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Game.Net.Ngo
{
    /// Сессия NGO: хост — это сервер и клиент в одном процессе, судья — он же. Правила контракта
    /// (не бросать, свой выход не Failed, отчёт следующим кадром) исполняет общий публикатор —
    /// здесь только факты стека.
    ///
    /// Список игроков и их вход-выход берутся у самого стека, а не собираются по аватарам:
    /// ConnectedClientsIds актуален и на клиенте, и о чужих подключениях он узнаёт сам
    /// (спайк, вопрос 5).
    public sealed class NgoSession : INetSession, IDisposable
    {
        private const string HOST_FAILED = "Не удалось поднять хост";
        private const string CONNECT_FAILED = "Не удалось начать подключение";
        private const string NO_ANSWER = "Хост не ответил за 5 секунд";
        private const string DISCONNECTED = "Хост недоступен или отключился";
        private const string BAD_ADDRESS = "Непонятный адрес: ";
        private const string LISTEN_ON_ALL = "0.0.0.0";

        /// Молчаливое зависание в лобби — худший вид ошибки в сетевом приложении, поэтому
        /// таймаут живёт здесь, а не в презентере.
        private static readonly TimeSpan CONNECT_TIMEOUT = TimeSpan.FromSeconds(5);

        private readonly NetworkManager _manager;
        private readonly NgoDirectory _directory;
        private readonly NgoSpawner _spawner;
        private readonly SessionStatePublisher _publisher;
        private readonly PlayerView _players;
        private readonly Subject<PlayerId> _joined = new();
        private readonly Subject<PlayerId> _left = new();

        /// Идёт ли наш матч. После собственного выхода стек ещё рассылает разрывы по каждому
        /// клиенту, и превращать их в уход игрока нельзя: логика механик тогда трогала бы
        /// сущности, которых уже нет.
        private bool _inSession;

        public ReadOnlyReactiveProperty<SessionState> State => _publisher.State;
        public PlayerId LocalPlayer => NgoIds.Player(_manager.LocalClientId);
        public bool IsJudge => _manager.IsServer;
        public IReadOnlyCollection<PlayerId> Players => _players;
        public Observable<PlayerId> PlayerJoined => _joined;
        public Observable<PlayerId> PlayerLeft => _left;

        public NgoSession(NetworkManager manager, NgoDirectory directory, NgoSpawner spawner, FrameProvider frames)
        {
            _manager = manager;
            _directory = directory;
            _spawner = spawner;
            _publisher = new SessionStatePublisher(frames);
            _players = new PlayerView(manager);
            _manager.OnConnectionEvent += OnConnectionEvent;
        }

        /// Отписка при уничтожении scope — то, ради чего сервисы и живут в контейнере.
        public void Dispose()
        {
            if (_manager != null) _manager.OnConnectionEvent -= OnConnectionEvent;

            _publisher.Dispose();
            _joined.Dispose();
            _left.Dispose();
        }

        /// Мир отдаём спавнеру до старта: место аватара NGO спрашивает уже внутри StartHost (И-2).
        public UniTask StartHostAsync(SessionSettings settings, INetWorld world, CancellationToken token)
        {
            _publisher.Begin(SessionPhase.Hosting);
            _spawner.Use(world);
            return _publisher.GuardAsync(() => HostAsync(settings));
        }

        public UniTask JoinAsync(HostEntry entry, INetWorld world, CancellationToken token)
        {
            _publisher.Begin(SessionPhase.Connecting);
            _spawner.Use(world);
            return _publisher.GuardAsync(() => ConnectAsync(entry, token));
        }

        /// Публикатор закрывается раньше Shutdown: разрывы, которые стек разошлёт следом,
        /// не должны стать ложным «хост отключился» на выходе в лобби (И-10).
        public UniTask LeaveAsync()
        {
            _inSession = false;
            _directory.StopAdvertising();
            _publisher.End();
            _manager.Shutdown();
            _spawner.Forget();
            return UniTask.CompletedTask;
        }

        private UniTask HostAsync(SessionSettings settings)
        {
            _manager.GetComponent<UnityTransport>()
                .SetConnectionData(NgoAddress.LOCALHOST, NgoAddress.DEFAULT_PORT, LISTEN_ON_ALL);
            _inSession = true;

            if (!_manager.StartHost()) throw new InvalidOperationException(HOST_FAILED);

            /// Порядок иной, чем у «Без сети»: свой аватар хост получает внутри StartHost,
            /// подтверждением подключения, — то есть раньше сущностей мира.
            _spawner.PopulateWorld();
            _directory.Advertise(settings);
            return UniTask.CompletedTask;
        }

        private async UniTask ConnectAsync(HostEntry entry, CancellationToken token)
        {
            if (!NgoAddress.TryParse(entry.JoinToken, out var address, out var port))
            {
                throw new InvalidOperationException(BAD_ADDRESS + entry.JoinToken);
            }

            _manager.GetComponent<UnityTransport>().SetConnectionData(address, port);
            _inSession = true;

            if (!_manager.StartClient()) throw new InvalidOperationException(CONNECT_FAILED);

            /// Первое значение при подписке — то, что в свойстве уже лежит: публикатор доносит
            /// наш Connecting только следующим кадром. Его пропускаем и ждём терминальную фазу,
            /// а не «любую, кроме Connecting», — иначе прошлое состояние сошло бы за ответ хоста.
            try
            {
                await _publisher.State
                    .Skip(1)
                    .Where(state => state.Phase is SessionPhase.Connected or SessionPhase.Failed)
                    .Timeout(CONNECT_TIMEOUT)
                    .FirstAsync(token);
            }
            catch (TimeoutException)
            {
                throw new InvalidOperationException(NO_ANSWER);
            }
        }

        private void OnConnectionEvent(NetworkManager manager, ConnectionEventData data)
        {
            if (!_inSession) return;

            if (_manager.IsServer)
            {
                OnServerEvent(data);
                return;
            }

            OnClientEvent(data);
        }

        /// Сервер получает на каждого клиента оба события — Client* и Peer* (спайк): считаем
        /// по Client*, иначе вход придёт дважды. Свой вход хосту новостью не является.
        private void OnServerEvent(ConnectionEventData data)
        {
            if (data.ClientId == _manager.LocalClientId) return;

            if (data.EventType == ConnectionEvent.ClientConnected) _joined.OnNext(NgoIds.Player(data.ClientId));
            if (data.EventType == ConnectionEvent.ClientDisconnected) _left.OnNext(NgoIds.Player(data.ClientId));
        }

        /// Клиенту о чужих рассказывают Peer*, о нём самом — Client*: своё подключение — это
        /// фаза сессии, а свой разрыв — отказ с причиной от стека.
        private void OnClientEvent(ConnectionEventData data)
        {
            switch (data.EventType)
            {
                case ConnectionEvent.ClientConnected:
                    _publisher.Report(new SessionState(SessionPhase.Connected));
                    return;
                case ConnectionEvent.ClientDisconnected:
                    _publisher.Report(new SessionState(SessionPhase.Failed, Reason()));
                    return;
                case ConnectionEvent.PeerConnected:
                    _joined.OnNext(NgoIds.Player(data.ClientId));
                    return;
                case ConnectionEvent.PeerDisconnected:
                    _left.OnNext(NgoIds.Player(data.ClientId));
                    return;
            }
        }

        /// Отказ подтверждения приходит с текстом от хоста — его игроку и показываем.
        private string Reason()
        {
            var reason = _manager.DisconnectReason;
            return string.IsNullOrEmpty(reason) ? DISCONNECTED : reason;
        }

        /// Копию списка держать нельзя: объекты у клиента появляются раньше его собственного
        /// ClientConnected (спайк), и логика механик спрашивает «кто в сессии» до первого
        /// события. Поэтому перевод в PlayerId идёт поверх живого списка стека.
        private sealed class PlayerView : IReadOnlyCollection<PlayerId>
        {
            private readonly NetworkManager _manager;

            public int Count => _manager.ConnectedClientsIds.Count;

            public PlayerView(NetworkManager manager)
            {
                _manager = manager;
            }

            public IEnumerator<PlayerId> GetEnumerator()
            {
                foreach (var clientId in _manager.ConnectedClientsIds)
                {
                    yield return NgoIds.Player(clientId);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
