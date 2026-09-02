using System.Globalization;

namespace Game.Core
{
    /// Формат: MAGIC|имя|игроков|максимум|стек|токен.
    /// Разделитель вырезается из имени при кодировании — иначе чужой ник ломает разбор.
    public static class LanBeaconCodec
    {
        public const string Magic = "MPDEMO1";

        private const char Separator = '|';

        public static string Encode(HostEntry entry)
        {
            var name = entry.Name.Replace(Separator, ' ');
            return string.Join(Separator.ToString(),
                Magic, name,
                entry.Players.ToString(CultureInfo.InvariantCulture),
                entry.MaxPlayers.ToString(CultureInfo.InvariantCulture),
                entry.StackId, entry.JoinToken);
        }

        public static bool TryDecode(string raw, out HostEntry entry)
        {
            entry = null;
            if (string.IsNullOrEmpty(raw)) return false;

            var parts = raw.Split(Separator);
            if (parts.Length != 6 || parts[0] != Magic) return false;
            if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var players)) return false;
            if (!int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxPlayers)) return false;

            entry = new HostEntry(parts[1], players, maxPlayers, parts[4], parts[5]);
            return true;
        }
    }
}
