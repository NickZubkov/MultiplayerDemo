using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// Живёт в сцене арены: собирает маркеры при загрузке и отдаёт их как ISpawnPointRegistry.
    /// Статические списки в маркерах не нужны.
    ///
    /// Регистрацией занимается ArenaScope, а не этот класс: он только про точки,
    /// поэтому и называется по содержимому, а не по роли в контейнере.
    public sealed class ArenaSpawnPoints : MonoBehaviour, ISpawnPointRegistry
    {
        private readonly List<SpawnPoint> _players = new();
        private readonly List<SpawnPoint> _items = new();

        public IReadOnlyList<SpawnPoint> Players => _players;
        public IReadOnlyList<SpawnPoint> Items => _items;

        private void Awake()
        {
            foreach (var marker in FindObjectsByType<SpawnPointMarker>(FindObjectsSortMode.None))
            {
                var point = new SpawnPoint(marker.transform.position, marker.transform.rotation);

                if (marker.Kind == SpawnKind.Player)
                {
                    _players.Add(point);
                }
                else
                {
                    _items.Add(point);
                }
            }
        }
    }
}
