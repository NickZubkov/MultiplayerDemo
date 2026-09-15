using Game.Net;

namespace Game.Core
{
    /// Отказ уходит только тому, кто просил.
    public struct PickupDenied : INetMessage
    {
        private PickupDenial _reason;

        public PickupDenial Reason => _reason;

        public PickupDenied(PickupDenial reason)
        {
            _reason = reason;
        }

        public void Write(ref NetWriter writer) => writer.WriteByte((byte)_reason);

        public void Read(ref NetReader reader) => _reason = (PickupDenial)reader.ReadByte();
    }
}
