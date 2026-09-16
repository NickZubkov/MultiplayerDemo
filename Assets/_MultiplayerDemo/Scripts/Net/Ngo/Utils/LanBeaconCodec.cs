using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Game.Net.Ngo
{
    /// Формат маяка NGO: MAGIC|стек|имя|игроков|максимум|токен|ключ=значение|ключ=значение|…
    /// Формат не общий для LAN-стеков, а свой: у Mirror поиск из коробки и другой пакет (И-4).
    ///
    /// Метаданные едут парами и без разбора: сеть возит словарь и не знает, что игра положила
    /// туда уровень. Поэтому пар может быть сколько угодно, включая ноль, — и их число не
    /// участвует в проверке пакета.
    ///
    /// Идентификатор стека — отдельное поле, а не метаданные: он нужен приёмнику раньше записи,
    /// чтобы отбросить чужой маяк, и в HostEntry ему места нет (спека § 5.4).
    ///
    /// Версия в магии поднимается вместе с форматом: пакет сборки с прежним набором полей
    /// должен отвергаться решением, а не случайно — по числу полей.
    public static class LanBeaconCodec
    {
        public const string MAGIC = "MPDEMO3";

        private const char SEPARATOR = '|';
        private const char PAIR = '=';

        /// MAGIC, стек, имя, игроки, максимум, токен. Всё, что дальше, — метаданные.
        private const int FIXED_FIELDS = 6;

        public static string Encode(string stackId, HostEntry entry)
        {
            var text = new StringBuilder();
            text.Append(MAGIC).Append(SEPARATOR);
            text.Append(Clean(stackId)).Append(SEPARATOR);
            text.Append(Clean(entry.Name)).Append(SEPARATOR);
            text.Append(entry.Players.ToString(CultureInfo.InvariantCulture)).Append(SEPARATOR);
            text.Append(entry.MaxPlayers.ToString(CultureInfo.InvariantCulture)).Append(SEPARATOR);
            text.Append(Clean(entry.JoinToken));

            foreach (var pair in entry.Metadata)
            {
                text.Append(SEPARATOR).Append(Clean(pair.Key).Replace(PAIR, ' '));
                text.Append(PAIR).Append(Clean(pair.Value));
            }

            return text.ToString();
        }

        public static bool TryDecode(string raw, out string stackId, out HostEntry entry)
        {
            stackId = null;
            entry = null;
            if (string.IsNullOrEmpty(raw)) return false;

            var parts = raw.Split(SEPARATOR);
            if (parts.Length < FIXED_FIELDS || parts[0] != MAGIC) return false;
            if (!int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var players)) return false;
            if (!int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxPlayers)) return false;

            stackId = parts[1];
            entry = new HostEntry(parts[2], players, maxPlayers, parts[5], MetadataOf(parts));
            return true;
        }

        /// Значение может содержать «=», ключ — нет: режем по первому разделителю пары.
        private static IReadOnlyDictionary<string, string> MetadataOf(string[] parts)
        {
            var metadata = new Dictionary<string, string>();

            for (var i = FIXED_FIELDS; i < parts.Length; i++)
            {
                var split = parts[i].IndexOf(PAIR);
                if (split <= 0) continue;

                metadata[parts[i].Substring(0, split)] = parts[i].Substring(split + 1);
            }

            return metadata;
        }

        /// Разделитель вырезается при кодировании — иначе чужой ник ломает разбор.
        private static string Clean(string text) => text == null ? string.Empty : text.Replace(SEPARATOR, ' ');
    }
}
