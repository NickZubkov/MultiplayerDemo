using System.Collections.Generic;
using Game.Net;
using Game.Net.Ngo;
using NUnit.Framework;

namespace Game.Tests
{
    /// Формат маяка — деталь NGO, поэтому и тесты его лежат при стеке (И-4). Ключ «arena»
    /// здесь литерал: кодек про уровни не знает и возит словарь как есть.
    public sealed class LanBeaconCodecTests
    {
        private const string ARENA_KEY = "arena";

        [Test]
        public void RoundTripPreservesAllFields()
        {
            var source = new HostEntry("Коля", 2, 4, "192.168.0.2:7777", Metadata("yard"));
            Assert.IsTrue(LanBeaconCodec.TryDecode(LanBeaconCodec.Encode("ngo", source), out var stackId,
                out var decoded));
            Assert.AreEqual("ngo", stackId);
            Assert.AreEqual("Коля", decoded.Name);
            Assert.AreEqual(2, decoded.Players);
            Assert.AreEqual(4, decoded.MaxPlayers);
            Assert.AreEqual("192.168.0.2:7777", decoded.JoinToken);
            Assert.AreEqual("yard", decoded.MetadataValue(ARENA_KEY));
        }

        /// Метаданные непрозрачны: их может не быть вовсе, и пакет всё равно пакет.
        [Test]
        public void EntryWithoutMetadataSurvives()
        {
            var source = new HostEntry("Коля", 1, 4, "192.168.0.2:7777", null);
            Assert.IsTrue(LanBeaconCodec.TryDecode(LanBeaconCodec.Encode("ngo", source), out _, out var decoded));
            Assert.AreEqual(0, decoded.Metadata.Count);
        }

        [Test]
        public void UnknownMetadataKeysSurvive()
        {
            var metadata = new Dictionary<string, string> { [ARENA_KEY] = "box", ["mode"] = "demo" };
            var source = new HostEntry("Коля", 1, 4, "192.168.0.2:7777", metadata);
            Assert.IsTrue(LanBeaconCodec.TryDecode(LanBeaconCodec.Encode("ngo", source), out _, out var decoded));
            Assert.AreEqual("box", decoded.MetadataValue(ARENA_KEY));
            Assert.AreEqual("demo", decoded.MetadataValue("mode"));
        }

        [Test]
        public void ForeignTrafficIsRejected() =>
            Assert.IsFalse(LanBeaconCodec.TryDecode("какой-то мусор", out _, out _));

        [Test]
        public void EmptyPayloadIsRejected() => Assert.IsFalse(LanBeaconCodec.TryDecode("", out _, out _));

        [Test]
        public void TruncatedPayloadIsRejected() =>
            Assert.IsFalse(LanBeaconCodec.TryDecode(LanBeaconCodec.MAGIC + "|ngo|Коля", out _, out _));

        /// Пакет сборки с прежним форматом отсекается по магии, а не по числу полей:
        /// отказ должен быть решением, а не побочным следствием длины.
        [Test]
        public void PacketOfOlderBuildIsRejected() =>
            Assert.IsFalse(LanBeaconCodec.TryDecode("MPDEMO2|Коля|2|4|ngo|192.168.0.2:7777|box", out _, out _));

        [Test]
        public void SeparatorInNameDoesNotBreakParsing()
        {
            var source = new HostEntry("Ко|ля", 1, 4, "192.168.0.2:7777", Metadata("box"));
            Assert.IsTrue(LanBeaconCodec.TryDecode(LanBeaconCodec.Encode("ngo", source), out _, out var decoded));
            Assert.AreEqual("192.168.0.2:7777", decoded.JoinToken);
            Assert.AreEqual("box", decoded.MetadataValue(ARENA_KEY));
        }

        private static IReadOnlyDictionary<string, string> Metadata(string arenaId) =>
            new Dictionary<string, string> { [ARENA_KEY] = arenaId };
    }
}
