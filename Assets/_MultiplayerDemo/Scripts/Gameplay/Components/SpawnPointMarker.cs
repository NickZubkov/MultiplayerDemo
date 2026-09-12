using UnityEngine;

namespace Game.Gameplay
{
    /// Без статики: маркеры собирает инсталлер арены, а не они сами себя регистрируют.
    public sealed class SpawnPointMarker : MonoBehaviour
    {
        [SerializeField] private SpawnKind _kind = SpawnKind.Player;

        public SpawnKind Kind => _kind;
    }
}
