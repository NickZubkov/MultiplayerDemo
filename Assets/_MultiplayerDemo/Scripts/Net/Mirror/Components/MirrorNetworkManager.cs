using Mirror;

namespace Game.Net.Mirror
{
    /// Мост между менеджером Mirror и сервисами scope. Отдельный наследник нужен потому,
    /// что Mirror отдаёт и события подключения, и создание игрока переопределяемыми
    /// методами менеджера: NetworkClient.OnConnectedEvent он присваивает себе сам в
    /// RegisterClientMessages, и чужая подписка там не выживет.
    ///
    /// Зависимости приходят вызовом Bind из RegisterBuildCallback, а не через [Inject]:
    /// MirrorSession просит менеджер конструктором, и инъекция в обратную сторону
    /// замкнула бы кольцо, на котором сборка контейнера падает.
    ///
    /// В сцене у менеджера снят dontDestroyOnLoad: сцену стека выгружают при возврате к
    /// выбору стека, и менеджер обязан уйти вместе с ней. Статический singleton при этом
    /// не мешает — новый экземпляр забирает его себе (NetworkManager.InitializeSingleton).
    public sealed class MirrorNetworkManager : NetworkManager
    {
        private MirrorSession _session;
        private MirrorSpawner _spawner;
        private MirrorPlayers _players;
        private bool _quitting;

        public void Bind(MirrorSession session, MirrorSpawner spawner, MirrorPlayers players)
        {
            _session = session;
            _spawner = spawner;
            _players = players;
        }

        /// Флаг ставим до базы: внутри она зовёт StopClient, и разрыв прилетит уже во время
        /// закрытия приложения. Разбирать его как отказ сессии нельзя — презентер начал бы
        /// показывать сообщение и выгружать арену в уходящем процессе.
        public override void OnApplicationQuit()
        {
            _quitting = true;
            base.OnApplicationQuit();
        }

        /// ReplaceHandler, а не RegisterHandler: вторая сессия того же scope снова пройдёт
        /// через OnStartClient.
        public override void OnStartClient()
        {
            _spawner.RegisterHandlers();
            NetworkClient.ReplaceHandler<MirrorWelcome>(welcome => _players.Greet(new PlayerId(welcome.Player)));
        }

        public override void OnStopClient()
        {
            _spawner.UnregisterHandlers();
            NetworkClient.UnregisterHandler<MirrorWelcome>();
        }

        public override void OnClientConnect()
        {
            /// База здесь не косметика: именно она объявляет клиента готовым и просит
            /// сервер создать игрока. Без неё подключение проходит, а игрока нет.
            base.OnClientConnect();
            _session.ReportConnected();
        }

        public override void OnClientDisconnect()
        {
            if (_quitting) return;

            _session.ReportLost(null);
        }

        /// Номер игрока выдаёт сервер и говорит его первым сообщением: connectionId у KCP —
        /// хеш адреса и бывает отрицательным (спайк), а PlayerId отрицательных не принимает.
        /// Приветствие идёт тем же надёжным каналом раньше любого спавна, поэтому к появлению
        /// своего аватара клиент уже знает, кто он.
        public override void OnServerConnect(NetworkConnectionToClient connection)
        {
            base.OnServerConnect(connection);

            var player = _players.Admit(connection);
            connection.Send(new MirrorWelcome { Player = player.Value });
        }

        /// Номер снимаем до базы: она уничтожит аватар, и носитель уйдёт уже без игрока.
        /// Сущности мира ушедший не держит — они не его, и Mirror их не тронет.
        public override void OnServerDisconnect(NetworkConnectionToClient connection)
        {
            _players.Release(connection);
            base.OnServerDisconnect(connection);
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient connection) =>
            _spawner.SpawnAvatar(connection);
    }
}
