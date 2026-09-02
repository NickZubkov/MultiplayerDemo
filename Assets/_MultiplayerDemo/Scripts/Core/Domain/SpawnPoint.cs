using UnityEngine;

namespace Game.Core
{
    public readonly struct SpawnPoint
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public SpawnPoint(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}
