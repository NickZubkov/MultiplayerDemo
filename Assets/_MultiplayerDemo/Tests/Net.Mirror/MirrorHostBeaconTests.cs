using System.Collections.Generic;
using Game.Net;
using Game.Net.Mirror;
using NUnit.Framework;

namespace Game.Tests
{
    /// Метаданные едут в маяке Mirror двумя массивами: веавер пишет массивы, но не словари.
    /// Перевод туда и обратно — деталь стека, поэтому и тесты его лежат при стеке. Ключ «arena»
    /// здесь литерал: сеть возит словарь как есть и про уровни не знает.
    public sealed class MirrorHostBeaconTests
    {
        private const string ARENA_KEY = "arena";

        [Test]
        public void MetadataSurvivesThePairs()
        {
            var settings = new SessionSettings("Коля", 4, new Dictionary<string, string> { [ARENA_KEY] = "yard" });

            var beacon = MirrorHostBeacon.Of(settings);

            Assert.AreEqual("Коля", beacon.HostName);
            Assert.AreEqual(4, beacon.MaxPlayers);
            Assert.AreEqual("yard", beacon.Metadata()[ARENA_KEY]);
        }

        /// Метаданные непрозрачны: их может не быть вовсе, и ответ всё равно ответ.
        [Test]
        public void BeaconWithoutMetadataIsEmpty()
        {
            var beacon = MirrorHostBeacon.Of(new SessionSettings("Коля", 4, null));

            Assert.IsEmpty(beacon.Metadata());
        }

        /// Пакет пришёл по сети, и массивы в нём могли разъехаться — читаем по короткому.
        [Test]
        public void MismatchedPairsAreReadByTheShorter()
        {
            var beacon = new MirrorHostBeacon
            {
                MetadataKeys = new[] { ARENA_KEY, "mode" },
                MetadataValues = new[] { "yard" },
            };

            var metadata = beacon.Metadata();

            Assert.AreEqual(1, metadata.Count);
            Assert.AreEqual("yard", metadata[ARENA_KEY]);
        }

        /// Счётчик игроков в шаблоне ответа не заполняется: его проставляет каталог на каждой
        /// отправке, иначе хост показывал бы в чужом списке единицу с момента старта (И-7).
        [Test]
        public void PlayersAreNotTakenFromSettings()
        {
            Assert.AreEqual(0, MirrorHostBeacon.Of(new SessionSettings("Коля", 4, null)).Players);
        }
    }
}
