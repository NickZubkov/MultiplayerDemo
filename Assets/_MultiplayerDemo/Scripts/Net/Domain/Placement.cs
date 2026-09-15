using UnityEngine;

namespace Game.Net
{
    /// Динамическая сущность, поставленная дизайнером в сцену: сама копия при загрузке
    /// убирается, а судья создаёт на её месте сетевую.
    public readonly struct Placement
    {
        public readonly string EntityType;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public Placement(string entityType, Vector3 position, Quaternion rotation)
        {
            EntityType = entityType;
            Position = position;
            Rotation = rotation;
        }
    }
}
