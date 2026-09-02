namespace Game.Core
{
    /// Снимок обстановки на момент попытки взятия. Структура readonly и без
    /// ссылок на сцену — только так правила остаются проверяемыми в EditMode.
    public readonly struct PickupQuery
    {
        public readonly bool ItemFree;
        public readonly bool HandsEmpty;
        public readonly float Distance;
        public readonly bool HasLineOfSight;

        public PickupQuery(bool itemFree, bool handsEmpty, float distance, bool hasLineOfSight)
        {
            ItemFree = itemFree;
            HandsEmpty = handsEmpty;
            Distance = distance;
            HasLineOfSight = hasLineOfSight;
        }
    }
}
