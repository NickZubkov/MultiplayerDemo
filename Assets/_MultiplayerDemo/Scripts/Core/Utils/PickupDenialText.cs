namespace Game.Core
{
    /// Текст отказа один на все три стека: адаптер присылает игроку причину,
    /// а не готовую строку — иначе формулировка разъехалась бы по трём реализациям.
    public static class PickupDenialText
    {
        public static string Describe(PickupDenial denial)
        {
            switch (denial)
            {
                case PickupDenial.ItemHeld: return "Предмет уже в чужих руках";
                case PickupDenial.HandsBusy: return "Руки заняты";
                case PickupDenial.TooFar: return "Слишком далеко";
                case PickupDenial.NoLineOfSight: return "Предмет не виден";
                default: return string.Empty;
            }
        }
    }
}
