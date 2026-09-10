using Fusion;
using Game.Core;
using UnityEngine;

namespace Game.Net.Fusion
{
    /// Арена не знает о сети: в ней только маркеры. Но кто наполняет мир — здесь другое,
    /// чем в NGO и Mirror: сервера нет, и обязанности разделены. Ящики создаёт мастер-клиент
    /// комнаты, то есть тот, кто её создал, а аватар каждый создаёт себе сам.
    public sealed class FusionWorldSpawner : IWorldSpawner
    {
        private readonly FusionRunnerFactory _runners;
        private readonly GameObject _playerPrefab;
        private readonly GameObject _cratePrefab;

        private ISpawnPointRegistry _points;

        public FusionWorldSpawner(FusionRunnerFactory runners, GameObject playerPrefab, GameObject cratePrefab)
        {
            _runners = runners;
            _playerPrefab = playerPrefab;
            _cratePrefab = cratePrefab;
        }

        public void UsePoints(ISpawnPointRegistry points) => _points = points;

        public void SpawnItems()
        {
            var runner = _runners.Live;
            if (runner == null || !runner.IsSharedModeMasterClient || _points == null) return;

            foreach (var point in _points.Items)
            {
                runner.Spawn(_cratePrefab, point.Position, point.Rotation);
            }
        }

        /// Зовёт мост, когда в сессию вошёл игрок. Свой аватар создаём только на входе
        /// самого себя: чужие сделают то же у себя, и власть над состоянием останется
        /// у владельца — договариваться, как в клиент-серверных стеках, не с кем.
        public void SpawnPlayer(NetworkRunner runner, PlayerRef player)
        {
            if (player != runner.LocalPlayer) return;

            var point = PointFor(player);
            runner.Spawn(_playerPrefab, point.Position, point.Rotation, player);
        }

        /// Точку выбираем по номеру игрока, а не по собственному счётчику, как в двух других
        /// стеках: счётчик там ведёт единственный авторитет, а здесь он был бы у каждого свой,
        /// и все встали бы в одну точку.
        private SpawnPoint PointFor(PlayerRef player)
        {
            var points = _points?.Players;
            if (points == null || points.Count == 0) return new SpawnPoint(Vector3.zero, Quaternion.identity);

            return points[Mathf.Abs(player.PlayerId) % points.Count];
        }
    }
}
