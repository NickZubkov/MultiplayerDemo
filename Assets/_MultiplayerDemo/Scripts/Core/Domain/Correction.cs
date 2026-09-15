using Game.Net;
using UnityEngine;

namespace Game.Core
{
    /// Поправка анти-телепорта владельцу аватара: куда вернуться.
    public struct Correction : INetMessage
    {
        private Vector3 _position;

        public Vector3 Position => _position;

        public Correction(Vector3 position)
        {
            _position = position;
        }

        public void Write(ref NetWriter writer) => writer.WriteVector3(_position);

        public void Read(ref NetReader reader) => _position = reader.ReadVector3();
    }
}
