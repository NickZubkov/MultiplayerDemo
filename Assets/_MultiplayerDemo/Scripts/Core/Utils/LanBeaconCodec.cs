using System.Globalization;
using Game.Net;

namespace Game.Core
{
    /// Формат: MAGIC|имя|игроков|максимум|стек|токен|арена.
    /// Разделитель вырезается из имени при кодировании — иначе чужой ник ломает разбор.
    ///
    /// Версия в магии поднимается вместе с форматом: пакет сборки без уровня должен
    /// отвергаться решением, а не случайно — по числу полей.
    public static class LanBeaconCodec
    {
        public const string MAGIC = "MPDEMO2";

        private const char SEPARATOR = '|';

        public static string Encode(HostEntry entry)
        {
            var name = entry.Name.Replace(SEPARATOR, ' ');
            return string.Join(SEPARATOR.ToString(),
                MAGIC, name,
                entry.Players.ToString(CultureInfo.InvariantCulture),
                entry.MaxPlayers.ToString(CultureInfo.InvariantCulture),
                entry.StackId, entry.JoinToken, entry.ArenaId);
        }

        public static bool TryDecode(string raw, out HostEntry entry)
        {
            entry = null;
            if (string.IsNullOrEmpty(raw)) return false;

            var parts = raw.Split(SEPARATOR);
            if (parts.Length != 7 || parts[0] != MAGIC) return false;
            if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var players)) return false;
            if (!int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxPlayers)) return false;

            entry = new HostEntry(parts[1], players, maxPlayers, parts[4], parts[5], parts[6]);
            return true;
        }
    }
}
