namespace Game.Net.Ngo
{
    /// JoinToken у NGO — «адрес:порт». Разбирают его двое: сессия перед подключением и каталог,
    /// когда игрок ввёл адрес руками. Разбор один на обоих — иначе лобби приняло бы строку,
    /// на которой сессия потом споткнулась бы.
    public static class NgoAddress
    {
        /// Порт хоста фиксирован: демка не даёт его выбрать, а маяк несёт адрес целиком.
        public const ushort DEFAULT_PORT = 7777;

        /// Адрес хоста для себя самого и запасной, когда резолвер не назвал ни одного IPv4.
        public const string LOCALHOST = "127.0.0.1";

        private const char SEPARATOR = ':';

        public static string Token(string address, ushort port) => $"{address}{SEPARATOR}{port}";

        /// Порт необязателен: игрок вводит «192.168.0.2», и это наш порт по умолчанию.
        public static bool TryParse(string text, out string address, out ushort port)
        {
            address = null;
            port = DEFAULT_PORT;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var trimmed = text.Trim();
            var separator = trimmed.IndexOf(SEPARATOR);

            if (separator < 0)
            {
                address = trimmed;
                return true;
            }

            if (separator == 0) return false;
            if (!ushort.TryParse(trimmed.Substring(separator + 1), out port)) return false;

            address = trimmed.Substring(0, separator);
            return true;
        }
    }
}
