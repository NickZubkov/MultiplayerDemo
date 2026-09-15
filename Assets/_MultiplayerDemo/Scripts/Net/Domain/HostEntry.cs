namespace Game.Net
{
    /// JoinToken непрозрачен: для LAN это "ip:port", для Fusion — имя сессии.
    /// UI отдаёт токен обратно тому адаптеру, который выдал запись, и не разбирает его.
    ///
    /// ArenaId едет рядом с адресом, потому что уровень выбирает хост: клиент грузит
    /// ту же арену, иначе ящики хоста повиснут в воздухе над чужим полом. Пустым поле
    /// остаётся только при ручном вводе адреса — там маяка не было и спросить некого.
    public sealed class HostEntry
    {
        public string Name { get; }
        public int Players { get; }
        public int MaxPlayers { get; }
        public string StackId { get; }
        public string JoinToken { get; }
        public string ArenaId { get; }

        public HostEntry(string name, int players, int maxPlayers, string stackId, string joinToken, string arenaId)
        {
            Name = name;
            Players = players;
            MaxPlayers = maxPlayers;
            StackId = stackId;
            JoinToken = joinToken;
            ArenaId = arenaId;
        }
    }
}
