using System.Collections.Generic;

namespace Game.Net
{
    /// JoinToken непрозрачен: для LAN это "ip:port", для Fusion — имя сессии; разбирает его
    /// тот стек, что выдал запись. Метаданные тоже непрозрачны — уровень лежит там под ключом
    /// игры, и сеть не знает, что это уровень (спека § 5.3).
    public sealed class HostEntry
    {
        private static readonly IReadOnlyDictionary<string, string> NO_METADATA = new Dictionary<string, string>();

        public string Name { get; }
        public int Players { get; }
        public int MaxPlayers { get; }
        public string JoinToken { get; }
        public IReadOnlyDictionary<string, string> Metadata { get; }

        public HostEntry(string name, int players, int maxPlayers, string joinToken,
            IReadOnlyDictionary<string, string> metadata)
        {
            Name = name;
            Players = players;
            MaxPlayers = maxPlayers;
            JoinToken = joinToken;
            Metadata = metadata ?? NO_METADATA;
        }

        public string MetadataValue(string key) => Metadata.TryGetValue(key, out var value) ? value : null;
    }
}
