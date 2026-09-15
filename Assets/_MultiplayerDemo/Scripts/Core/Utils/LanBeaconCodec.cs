using System.Collections.Generic;
using System.Globalization;
using Game.Net;

namespace Game.Core
{
    /// Формат: MAGIC|имя|игроков|максимум|стек|токен|арена.
    /// Разделитель вырезается из имени при кодировании — иначе чужой ник ломает разбор.
    ///
    /// Версия в магии поднимается вместе с форматом: пакет сборки без уровня должен
    /// отвергаться решением, а не случайно — по числу полей.
    ///
    /// Стек и уровень едут в метаданных записи: сеть возит словарь и не знает, что в нём.
    /// Поля пакета при этом остались прежними — формат менять было не за чем.
    public static class LanBeaconCodec
    {
        public const string MAGIC = "MPDEMO2";

        /// Ключ стека в метаданных. Уровень лежит под ArenaDefinition.METADATA_KEY.
        public const string STACK_METADATA_KEY = "stack";

        private const char SEPARATOR = '|';

        public static string Encode(HostEntry entry)
        {
            var name = entry.Name.Replace(SEPARATOR, ' ');
            return string.Join(SEPARATOR.ToString(),
                MAGIC, name,
                entry.Players.ToString(CultureInfo.InvariantCulture),
                entry.MaxPlayers.ToString(CultureInfo.InvariantCulture),
                entry.MetadataValue(STACK_METADATA_KEY), entry.JoinToken,
                entry.MetadataValue(ArenaDefinition.METADATA_KEY));
        }

        public static bool TryDecode(string raw, out HostEntry entry)
        {
            entry = null;
            if (string.IsNullOrEmpty(raw)) return false;

            var parts = raw.Split(SEPARATOR);
            if (parts.Length != 7 || parts[0] != MAGIC) return false;
            if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var players)) return false;
            if (!int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxPlayers)) return false;

            var metadata = new Dictionary<string, string>
            {
                [STACK_METADATA_KEY] = parts[4],
                [ArenaDefinition.METADATA_KEY] = parts[6],
            };

            entry = new HostEntry(parts[1], players, maxPlayers, parts[5], metadata);
            return true;
        }
    }
}
