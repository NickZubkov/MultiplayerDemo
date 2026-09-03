using System;
using Game.Core;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Net.Ngo
{
    /// Арена не знает о сети: в ней только маркеры, ящики создаёт хост.
    /// Он же расставляет игроков по точкам — место для автоспавна NGO принимает
    /// единственным способом, через подтверждение подключения, поэтому оно живёт здесь.
    public sealed class NgoWorldSpawner : IWorldSpawner, IStartable, IDisposable
    {
        private readonly IObjectResolver _resolver;
        private readonly NetworkManager _manager;
        private readonly GameObject _cratePrefab;

        private ISpawnPointRegistry _points;
        private int _nextPlayerPoint;

        public NgoWorldSpawner(IObjectResolver resolver, NetworkManager manager, GameObject cratePrefab)
        {
            _resolver = resolver;
            _manager = manager;
            _cratePrefab = cratePrefab;
        }

        public void Start() => _manager.ConnectionApprovalCallback = Approve;

        public void Dispose()
        {
            if (_manager != null) _manager.ConnectionApprovalCallback = null;
        }

        /// Точки нужны раньше сессии: место игрока NGO спрашивает уже внутри StartHost,
        /// а ящики появляются после — до старта сервера их некуда спавнить.
        public void UsePoints(ISpawnPointRegistry points) => _points = points;

        public void SpawnItems()
        {
            if (!_manager.IsServer || _points == null) return;

            foreach (var point in _points.Items)
            {
                var crate = _resolver.Instantiate(_cratePrefab, point.Position, point.Rotation);
                crate.GetComponent<NetworkObject>().Spawn();
            }
        }

        /// Отказов пока нет: подтверждение включено только ради позиции спавна.
        private void Approve(NetworkManager.ConnectionApprovalRequest request,
                             NetworkManager.ConnectionApprovalResponse response)
        {
            var point = NextPlayerPoint();

            response.Approved = true;
            response.CreatePlayerObject = true;
            response.Position = point.Position;
            response.Rotation = point.Rotation;
        }

        /// Простой круг по счётчику: точек на арене три, игроков до четырёх, и двое
        /// окажутся в одной точке только после переподключений — для демки достаточно.
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
