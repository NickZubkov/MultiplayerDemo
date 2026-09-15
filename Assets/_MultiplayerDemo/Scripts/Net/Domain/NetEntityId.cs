using System;

namespace Game.Net
{
    /// Сценовые идентификаторы постоянны (хеш GUID из сцены), динамические выдаёт стек.
    /// Старший бит разводит их, чтобы число от стека не совпало с хешем двери.
    public readonly struct NetEntityId : IEquatable<NetEntityId>
    {
        private const ulong DYNAMIC_BIT = 1ul << 63;

        public static readonly NetEntityId NONE = default;

        public readonly ulong Value;

        public bool IsNone => Value == 0;

        public NetEntityId(ulong value)
        {
            Value = value;
        }

        /// Младший бит взведён всегда: хеш, случайно давший ноль, не станет «никем».
        public static NetEntityId Scene(string sceneId) => new((StableHash.Of64(sceneId) & ~DYNAMIC_BIT) | 1ul);

        public static NetEntityId Dynamic(ulong raw) => new(raw | DYNAMIC_BIT);

        public static bool operator ==(NetEntityId left, NetEntityId right) => left.Value == right.Value;

        public static bool operator !=(NetEntityId left, NetEntityId right) => left.Value != right.Value;

        public bool Equals(NetEntityId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is NetEntityId other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value.ToString("X16");
    }
}
