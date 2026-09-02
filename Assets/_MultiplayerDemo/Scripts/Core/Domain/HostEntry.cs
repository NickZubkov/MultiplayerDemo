namespace Game.Core
{
    /// JoinToken непрозрачен: для LAN это "ip:port", для Fusion — имя сессии.
    /// UI отдаёт токен обратно тому адаптеру, который выдал запись, и не разбирает его.
    public sealed class HostEntry
    {
        public string Name { get; }
        public int Players { get; }
        public int MaxPlayers { get; }
        public string StackId { get; }
        public string JoinToken { get; }

        public HostEntry(string name, int players, int maxPlayers, string stackId, string joinToken)
        {
            Name = name;
            Players = players;
            MaxPlayers = maxPlayers;
            StackId = stackId;
            JoinToken = joinToken;
        }
    }
}
