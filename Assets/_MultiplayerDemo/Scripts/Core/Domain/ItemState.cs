namespace Game.Core
{
    /// Автомат «свободен ↔ в руках». Переходы разрешает только авторитетная сторона:
    /// хост в NGO и Mirror, владелец состояния в Fusion Shared.
    public sealed class ItemState
    {
        public ItemPhase Phase { get; private set; } = ItemPhase.Free;
        public ulong Holder { get; private set; }

        public bool TryHold(ulong playerId)
        {
            if (Phase == ItemPhase.Held) return false;
            Phase = ItemPhase.Held;
            Holder = playerId;
            return true;
        }

        public bool TryRelease(ulong playerId)
        {
            if (Phase != ItemPhase.Held || Holder != playerId) return false;
            Phase = ItemPhase.Free;
            Holder = 0;
            return true;
        }
    }
}
