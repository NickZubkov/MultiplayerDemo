using Game.Core;
using Mirror;
using UnityEngine;

namespace Game.Net.Mirror
{
    /// Арена не знает о сети: в ней только маркеры, ящики создаёт хост. Он же
    /// расставляет игроков по точкам — Mirror по умолчанию берёт место из
    /// NetworkManager.startPositions, то есть из компонентов в сцене, о которых
    /// загрузчик арены ничего не знает.
    public sealed class MirrorWorldSpawner : IWorldSpawner
    {
        private readonly MirrorObjectFactory _factory;
        private readonly GameObject _playerPrefab;
        private readonly GameObject _cratePrefab;

        private ISpawnPointRegistry _points;
        private int _nextPlayerPoint;

        public MirrorWorldSpawner(MirrorObjectFactory factory, GameObject playerPrefab, GameObject cratePrefab)
        {
            _factory = factory;
            _playerPrefab = playerPrefab;
            _cratePrefab = cratePrefab;
        }

        /// Точки нужны раньше сессии: игрока Mirror просит создать сразу после
        /// подключения, а ящики появляются позже — до старта сервера их некуда спавнить.
        public void UsePoints(ISpawnPointRegistry points) => _points = points;

        public void SpawnItems()
        {
            if (!NetworkServer.active || _points == null) return;

            foreach (var point in _points.Items)
            {
                var crate = _factory.Create(_cratePrefab, point.Position, point.Rotation);
                NetworkServer.Spawn(crate);
            }
        }

        /// Зовёт мост из OnServerAddPlayer. Имя с номером подключения — привычка самого
        /// Mirror: без неё в иерархии сервера подряд стоят одинаковые «Player_Mirror(Clone)».
        public void SpawnPlayer(NetworkConnectionToClient connection)
        {
            var point = NextPlayerPoint();
            var player = _factory.Create(_playerPrefab, point.Position, point.Rotation);

            player.name = $"{_playerPrefab.name} [connId={connection.connectionId}]";
            NetworkServer.AddPlayerForConnection(connection, player);
        }

        /// Простой круг по счётчику: точек на арене три-четыре, игроков до четырёх,
        /// и двое окажутся в одной точке только после переподключений.
        private SpawnPoint NextPlayerPoint()
        {
            var points = _points?.Players;
            if (points == null || points.Count == 0) return new SpawnPoint(Vector3.zero, Quaternion.identity);

            var point = points[_nextPlayerPoint % points.Count];
            _nextPlayerPoint++;
            return point;
        }
    }
}
