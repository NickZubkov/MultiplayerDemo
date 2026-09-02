namespace Game.Core
{
    public enum PickupDenial { None, ItemHeld, HandsBusy, TooFar, NoLineOfSight }

    public readonly struct PickupQuery
    {
        public readonly bool  ItemFree;
        public readonly bool  HandsEmpty;
        public readonly float Distance;
        public readonly bool  HasLineOfSight;

        public PickupQuery(bool itemFree, bool handsEmpty, float distance, bool hasLineOfSight)
        {
            ItemFree       = itemFree;
            HandsEmpty     = handsEmpty;
            Distance       = distance;
            HasLineOfSight = hasLineOfSight;
        }
    }

    /// Единственное место, где сформулировано «можно ли взять».
    /// Порядок проверок задаёт приоритет причины отказа — её же видит игрок.
    public static class PickupRules
    {
        public const float MaxDistance = 2.5f;

        public static PickupDenial Evaluate(in PickupQuery q)
        {
            if (!q.ItemFree)              return PickupDenial.ItemHeld;
            if (!q.HandsEmpty)            return PickupDenial.HandsBusy;
            if (q.Distance > MaxDistance) return PickupDenial.TooFar;
            if (!q.HasLineOfSight)        return PickupDenial.NoLineOfSight;
            return PickupDenial.None;
        }
    }
}
