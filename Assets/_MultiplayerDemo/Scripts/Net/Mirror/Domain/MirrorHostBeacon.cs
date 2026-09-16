using System;
using System.Collections.Generic;
using System.Net;
using Mirror;

namespace Game.Net.Mirror
{
    /// Ответ хоста на запрос поиска. Штатный ServerResponse у Mirror несёт только адрес и id
    /// сервера, а лобби нужны имя, счёт игроков и метаданные: без уровня клиент не узнает,
    /// какую арену грузить, и приземлится в чужой пол.
    ///
    /// Метаданные едут двумя массивами, а не словарём: веавер Mirror пишет массивы и списки,
    /// но не словари (Writers.cs:98–122). Пары непрозрачны — что в них, знает игра, а не сеть.
    ///
    /// Поля публичные, а не свойства, — weaver генерирует сериализацию именно по полям
    /// (ReaderWriterProcessor.cs:116, по всем NetworkMessage сборки). Адрес поэтому объявлен
    /// свойством: его заполняет клиент из конверта пакета, слать его незачем.
    public struct MirrorHostBeacon : NetworkMessage
    {
        public IPEndPoint EndPoint { get; set; }

        public string HostName;
        public int Players;
        public int MaxPlayers;
        public ushort Port;
        public string[] MetadataKeys;
        public string[] MetadataValues;

        /// Счётчик игроков здесь не заполняется: он живой и проставляется на каждой отправке.
        public static MirrorHostBeacon Of(SessionSettings settings)
        {
            var count = settings.Metadata?.Count ?? 0;
            var keys = new string[count];
            var values = new string[count];
            var index = 0;

            if (settings.Metadata != null)
            {
                foreach (var pair in settings.Metadata)
                {
                    keys[index] = pair.Key;
                    values[index] = pair.Value;
                    index++;
                }
            }

            return new MirrorHostBeacon
            {
                HostName = settings.Name,
                MaxPlayers = settings.MaxPlayers,
                MetadataKeys = keys,
                MetadataValues = values,
            };
        }

        /// Пакет пришёл по сети, и его массивы могли разъехаться: читаем по короткому из двух.
        public IReadOnlyDictionary<string, string> Metadata()
        {
            var metadata = new Dictionary<string, string>();
            if (MetadataKeys == null || MetadataValues == null) return metadata;

            var count = Math.Min(MetadataKeys.Length, MetadataValues.Length);

            for (var i = 0; i < count; i++)
            {
                if (!string.IsNullOrEmpty(MetadataKeys[i])) metadata[MetadataKeys[i]] = MetadataValues[i];
            }

            return metadata;
        }
    }
}
