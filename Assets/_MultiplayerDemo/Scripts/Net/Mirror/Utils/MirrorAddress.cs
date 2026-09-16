using Mirror;

namespace Game.Net.Mirror
{
    /// JoinToken у Mirror — «адрес:порт». Разбирают его двое: сессия перед подключением и
    /// каталог, когда игрок ввёл адрес руками. Разбор один на обоих — иначе лобби приняло бы
    /// строку, на которой сессия потом споткнулась бы.
    ///
    /// Своей константы порта здесь нет (И-16, Ц17): порт живёт на транспорте в сцене, и тот же
    /// транспорт отвечает за него в маяке. Когда игрок вводит адрес без порта, подставляется
    /// порт нашего транспорта — другого умолчания у стека нет.
    public static class MirrorAddress
    {
        private const char SEPARATOR = ':';

        public static string Token(string address, ushort port) => $"{address}{SEPARATOR}{port}";

        public static ushort PortOf(Transport transport) =>
            transport is PortTransport port ? port.Port : (ushort)0;

        public static bool TryParse(string text, ushort fallbackPort, out string address, out ushort port)
        {
            address = null;
            port = fallbackPort;
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
