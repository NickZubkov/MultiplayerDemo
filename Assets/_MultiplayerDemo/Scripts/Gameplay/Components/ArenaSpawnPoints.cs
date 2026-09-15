using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// Точки спавна игроков своей сцены. Ящиков здесь больше нет: они стоят в сцене
    /// размещениями (NetEntity), а маркеров под них не нужно.
    public sealed class ArenaSpawnPoints : MonoBehaviour
    {
        private readonly List<SpawnPoint> _players = new();

        public IReadOnlyList<SpawnPoint> Players => _players;

        /// Обход корней своей сцены, включая выключенные объекты. FindObjectsByType искал по
        /// всем загруженным сценам и пропускал выключенные: при двух аренах разом точки
        /// перемешались бы (И-24).
        private void Awake()
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                foreach (var marker in root.GetComponentsInChildren<SpawnPointMarker>(true))
                {
                    _players.Add(new SpawnPoint(marker.transform.position, marker.transform.rotation));
                }
            }
        }
    }
}
