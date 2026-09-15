using UnityEngine;

namespace Game.Gameplay
{
    /// Точка спавна игрока; ящики — размещения. Без статики: маркеры собирает
    /// ArenaSpawnPoints, а не они сами себя регистрируют.
    public sealed class SpawnPointMarker : MonoBehaviour { }
}
