using Fusion;

namespace Game.Net.Fusion
{
    /// PlayerRef несёт номер игрока, одинаковый на всех машинах, но бывает и «никем» — у
    /// выключенного раннера, у выделенного сервера, у объекта без владельца ввода. PlayerId
    /// отрицательных не принимает, поэтому перевод живёт одним местом на весь стек, а не
    /// тремя копиями в носителе, сессии и спавнере.
    public static class FusionIds
    {
        public static PlayerId Player(PlayerRef player) =>
            player.IsRealPlayer ? new PlayerId(player.PlayerId) : PlayerId.NONE;
    }
}
