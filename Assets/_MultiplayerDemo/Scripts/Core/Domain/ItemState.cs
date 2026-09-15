using Game.Net;

namespace Game.Core
{
    /// Автомат «свободен ↔ в руках». Переходы разрешает только авторитет сущности —
    /// хост у NGO, Mirror и Local, мастер-клиент у Fusion (Ц7). Держатель — PlayerId:
    /// его «никто» не совпадает ни с одним игроком ни на одном стеке (И-1).
    public sealed class ItemState
    {
        public ItemPhase Phase { get; private set; } = ItemPhase.Free;
        public PlayerId Holder { get; private set; } = PlayerId.NONE;

        public bool TryHold(PlayerId player)
        {
            if (Phase == ItemPhase.Held || player.IsNone) return false;

            Phase = ItemPhase.Held;
            Holder = player;
            return true;
        }

        public bool TryRelease(PlayerId player)
        {
            if (Phase != ItemPhase.Held || Holder != player) return false;

            Phase = ItemPhase.Free;
            Holder = PlayerId.NONE;
            return true;
        }

        public void Restore(PlayerId holder)
        {
            Holder = holder;
            Phase = holder.IsNone ? ItemPhase.Free : ItemPhase.Held;
        }
    }
}
