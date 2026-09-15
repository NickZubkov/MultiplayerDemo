using Game.Net;
using UnityEngine;

namespace Game.Core
{
    /// Импульс считает отправитель по своему взгляду: тангаж чужой головы авторитету
    /// неизвестен. Авторитет ограничивает его конфигом.
    public struct DropCommand : INetMessage
    {
        private Vector3 _impulse;

        public Vector3 Impulse => _impulse;

        public DropCommand(Vector3 impulse)
        {
            _impulse = impulse;
        }

        public void Write(ref NetWriter writer) => writer.WriteVector3(_impulse);

        public void Read(ref NetReader reader) => _impulse = reader.ReadVector3();
    }
}
