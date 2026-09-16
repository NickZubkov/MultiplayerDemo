using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;

namespace Game.Net.Fusion
{
    /// Перевод коллбэков Fusion в наши примитивы. Отдельный класс, а не реализация прямо на
    /// сервисе: интерфейс раннера широкий — девятнадцать методов на все случаи от ввода до
    /// миграции хоста, — и держать эти заглушки внутри сессии или каталога значило бы прятать
    /// их работу за забором пустых тел.
    ///
    /// Компонента на префабе раннера для этого не нужно: NetworkRunner.AddCallbacks принимает
    /// любой объект, и мост остаётся обычным сервисом контейнера, которому не нужна сцена.
    ///
    /// OnUserSimulationMessage не реализован намеренно: это единственный метод интерфейса
    /// с телом по умолчанию, и сам Fusion пометил его как больше не используемый.
    public sealed class FusionRunnerCallbacks : INetworkRunnerCallbacks
    {
        private readonly FusionSession _session;
        private readonly FusionDirectory _directory;
        private readonly FusionSpawner _spawner;

        public FusionRunnerCallbacks(FusionSession session, FusionDirectory directory, FusionSpawner spawner)
        {
            _session = session;
            _directory = directory;
            _spawner = spawner;
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) =>
            _directory.OnSessionListUpdated(sessionList);

        /// В Shared Mode аватар себе создаёт каждый сам: сервера, который сделал бы это за всех,
        /// здесь нет, и власть над состоянием объекта получает тот, кто его создал.
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            _spawner.SpawnAvatar(runner, player);
            _session.ReportPlayerJoined(player);
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) => _session.ReportPlayerLeft(player);

        /// Единственный сигнал о конце сессии, который нужен лобби: разрыв связи Fusion тоже
        /// доводит до выключения раннера, отдельно слушать его незачем.
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) =>
            _session.ReportLost(shutdownReason);

        public void OnInput(NetworkRunner runner, NetworkInput input) { }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

        public void OnConnectedToServer(NetworkRunner runner) { }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request,
            byte[] token) { }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key,
            ReadOnlySpan<byte> data) { }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

        public void OnSceneLoadDone(NetworkRunner runner) { }

        public void OnSceneLoadStart(NetworkRunner runner) { }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    }
}
