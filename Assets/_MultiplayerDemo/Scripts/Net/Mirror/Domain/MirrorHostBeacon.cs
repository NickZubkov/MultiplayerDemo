using System.Net;
using Mirror;

namespace Game.Net.Mirror
{
    /// Ответ хоста на запрос поиска. Штатный ServerResponse у Mirror несёт только адрес
    /// и id сервера, а лобби нужны имя, счёт игроков и уровень: без ArenaId клиент не
    /// узнает, какую арену грузить, и приземлится в чужой пол.
    ///
    /// Поля публичные, а не свойства, — weaver генерирует сериализацию именно по полям
    /// (ReaderWriterProcessor.cs:116, по всем NetworkMessage сборки). Адрес поэтому
    /// объявлен свойством: его заполняет клиент из конверта пакета, слать его незачем.
    public struct MirrorHostBeacon : NetworkMessage
    {
        public IPEndPoint EndPoint { get; set; }

        public string HostName;
        public int Players;
        public int MaxPlayers;
        public ushort Port;
        public string ArenaId;
    }
}
