namespace Game.Core
{
    /// Причина, по которой взять предмет нельзя. Порядок членов совпадает
    /// с порядком проверок в PickupRules и задаёт приоритет отказа.
    public enum PickupDenial
    {
        None,
        ItemHeld,
        HandsBusy,
        TooFar,
        NoLineOfSight
    }
}
