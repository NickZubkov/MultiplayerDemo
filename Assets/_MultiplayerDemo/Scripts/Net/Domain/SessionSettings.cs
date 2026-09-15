using System.Collections.Generic;

namespace Game.Net
{
    /// Метаданные непрозрачны — игра кладёт туда arena, сеть доставляет словарь в каталог
    /// хостов (маяком, ответом поиска, свойствами комнаты Photon) и не знает, что это уровень.
    public sealed class SessionSettings
    {
        public string Name { get; }
        public int MaxPlayers { get; }
        public IReadOnlyDictionary<string, string> Metadata { get; }

        public SessionSettings(string name, int maxPlayers, IReadOnlyDictionary<string, string> metadata)
        {
            Name = name;
            MaxPlayers = maxPlayers;
            Metadata = metadata;
        }
    }
}
