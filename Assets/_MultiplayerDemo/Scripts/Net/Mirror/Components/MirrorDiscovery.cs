using System;
using System.Net;
using Mirror;
using Mirror.Discovery;

namespace Game.Net.Mirror
{
    /// Штатный поиск Mirror с расширенным ответом — ровно тот способ, который предлагает
    /// сам Mirror в шаблоне `56-Mirror__Network Discovery`: наследоваться от базы и
    /// объявить свои типы запроса и ответа.
    ///
    /// Запрос берём чужой, `ServerRequest`: клиенту нечего сказать о себе, а пустой
    /// структуре всё равно, в какой сборке её weaver обработал.
    ///
    /// В сцене у компонента два неочевидных значения. Порт 47778 вместо штатного 47777:
    /// на 47777 сидит собственный маяк NGO, а сокет Mirror заводится без ReuseAddress —
    /// два окна разных стеков на одной машине подрались бы за порт. Интервал опроса 1 с
    /// вместо трёх: TTL записи о хосте — 3 с (HostRegistry.TTL), при опросе раз в три
    /// секунды строка мигала бы от одной потерянной датаграммы.
    public sealed class MirrorDiscovery : NetworkDiscoveryBase<ServerRequest, MirrorHostBeacon>
    {
        private MirrorHostBeacon _own;

        public event Action<MirrorHostBeacon> HostFound;

        /// Что отвечать спрашивающим. Кладёт каталог хостов каждый кадр, пока идёт матч:
        /// счётчик игроков в ответе живой, а не снятый на старте (И-7).
        public void Describe(MirrorHostBeacon beacon) => _own = beacon;

        protected override ServerRequest GetRequest() => new ServerRequest();

        protected override MirrorHostBeacon ProcessRequest(ServerRequest request, IPEndPoint endpoint)
        {
            /// Порт спрашиваем у транспорта, а не держим константой: он настраивается
            /// в сцене, и разъехавшиеся значения дали бы клиенту адрес, по которому никого нет.
            var beacon = _own;
            beacon.Port = MirrorAddress.PortOf(transport);
            return beacon;
        }

        protected override void ProcessResponse(MirrorHostBeacon response, IPEndPoint endpoint)
        {
            /// Адрес берём из конверта: свой LAN-адрес хост достоверно не знает
            /// (их может быть несколько), а пакет пришёл именно оттуда.
            response.EndPoint = endpoint;
            HostFound?.Invoke(response);
        }
    }
}
