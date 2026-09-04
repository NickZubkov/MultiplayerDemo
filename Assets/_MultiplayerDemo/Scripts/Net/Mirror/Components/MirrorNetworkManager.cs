using Mirror;

namespace Game.Net.Mirror
{
    /// Мост между менеджером Mirror и сервисами scope. Отдельный наследник нужен потому,
    /// что Mirror отдаёт и события подключения, и создание игрока переопределяемыми
    /// методами менеджера: NetworkClient.OnConnectedEvent он присваивает себе сам в
    /// RegisterClientMessages, и чужая подписка там не выживет.
    ///
    /// Зависимости приходят вызовом Bind из RegisterBuildCallback, а не через [Inject]:
    /// MirrorSessionControl просит менеджер конструктором, и инъекция в обратную сторону
    /// замкнула бы кольцо, на котором сборка контейнера падает.
    ///
    /// В сцене у менеджера снят dontDestroyOnLoad: сцену стека выгружают при возврате к
    /// выбору стека, и менеджер обязан уйти вместе с ней. Статический singleton при этом
    /// не мешает — новый экземпляр забирает его себе (NetworkManager.InitializeSingleton).
    public sealed class MirrorNetworkManager : NetworkManager
    {
        private MirrorSessionControl _session;
        private MirrorWorldSpawner _spawner;
        private MirrorObjectFactory _factory;
        private bool _quitting;

        public void Bind(MirrorSessionControl session, MirrorWorldSpawner spawner, MirrorObjectFactory factory)
        {
            _session = session;
            _spawner = spawner;
            _factory = factory;
        }

        /// Флаг ставим до базы: внутри она зовёт StopClient, и разрыв прилетит уже во время
        /// закрытия приложения. Разбирать его как отказ сессии нельзя — презентер начал бы
        /// показывать сообщение и выгружать арену в уходящем процессе.
        public override void OnApplicationQuit()
        {
            _quitting = true;
            base.OnApplicationQuit();
        }

        public override void OnStartClient() => _factory.RegisterHandlers();

        public override void OnStopClient() => _factory.UnregisterHandlers();

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
            _session.ReportDisconnected(null);
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient connection) =>
            _spawner.SpawnPlayer(connection);

        /// Держатель уходит: предметы отпускаем до базы, потому что она уничтожит его
        /// объект, и netId в SyncVar ящика станет не с чем сопоставить — ящик повис бы
        /// занятым навсегда. Сами предметы Mirror не тронет: владение им не передаётся.
        public override void OnServerDisconnect(NetworkConnectionToClient connection)
        {
            if (connection.identity != null)
            {
                MirrorItem.ReleaseAllHeldBy(connection.identity.netId);
            }

            base.OnServerDisconnect(connection);
        }
    }
}
