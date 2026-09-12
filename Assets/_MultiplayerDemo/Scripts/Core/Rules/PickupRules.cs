namespace Game.Core
{
    /// Единственное место, где сформулировано «можно ли взять».
    /// Порядок проверок задаёт приоритет причины отказа — её же видит игрок.
    public static class PickupRules
    {
        public const float MAX_DISTANCE = 2.5f;

        public static PickupDenial Evaluate(in PickupQuery q)
        {
            if (!q.ItemFree) return PickupDenial.ItemHeld;
            if (!q.HandsEmpty) return PickupDenial.HandsBusy;
            if (q.Distance > MAX_DISTANCE) return PickupDenial.TooFar;
            if (!q.HasLineOfSight) return PickupDenial.NoLineOfSight;
            return PickupDenial.None;
        }
    }
}
