using UnityEngine;

namespace Game.Gameplay
{
    /// Без статики: маркеры собирает инсталлер арены, а не они сами себя регистрируют.
    public sealed class SpawnPointMarker : MonoBehaviour
    {
        [SerializeField] private SpawnKind kind = SpawnKind.Player;

        public SpawnKind Kind => kind;
    }
}
