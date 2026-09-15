using System.Collections.Generic;
using Game.Core;
using Game.Net;
using NUnit.Framework;

namespace Game.Tests
{
    public sealed class LanBeaconCodecTests
    {
        [Test]
        public void RoundTripPreservesAllFields()
        {
            var source = new HostEntry("Коля", 2, 4, "192.168.0.2:7777", Metadata("ngo", "yard"));
            Assert.IsTrue(LanBeaconCodec.TryDecode(LanBeaconCodec.Encode(source), out var decoded));
            Assert.AreEqual("Коля", decoded.Name);
            Assert.AreEqual(2, decoded.Players);
            Assert.AreEqual(4, decoded.MaxPlayers);
            Assert.AreEqual("ngo", decoded.MetadataValue(LanBeaconCodec.STACK_METADATA_KEY));
            Assert.AreEqual("192.168.0.2:7777", decoded.JoinToken);
            Assert.AreEqual("yard", decoded.MetadataValue(ArenaDefinition.METADATA_KEY));
        }

        [Test]
        public void ForeignTrafficIsRejected() => Assert.IsFalse(LanBeaconCodec.TryDecode("какой-то мусор", out _));

        [Test]
        public void EmptyPayloadIsRejected() => Assert.IsFalse(LanBeaconCodec.TryDecode("", out _));

        [Test]
        public void TruncatedPayloadIsRejected() =>
            Assert.IsFalse(LanBeaconCodec.TryDecode(LanBeaconCodec.MAGIC + "|Коля", out _));

        /// Пакет сборки без уровня отсекается по магии, а не по числу полей:
        /// отказ должен быть решением, а не побочным следствием длины.
        [Test]
        public void PacketOfOlderBuildIsRejected() =>
            Assert.IsFalse(LanBeaconCodec.TryDecode("MPDEMO1|Коля|2|4|ngo|192.168.0.2:7777", out _));

        [Test]
        public void SeparatorInNameDoesNotBreakParsing()
        {
            var source = new HostEntry("Ко|ля", 1, 4, "192.168.0.2:7777", Metadata("ngo", "box"));
            Assert.IsTrue(LanBeaconCodec.TryDecode(LanBeaconCodec.Encode(source), out var decoded));
            Assert.AreEqual("192.168.0.2:7777", decoded.JoinToken);
        }

        private static IReadOnlyDictionary<string, string> Metadata(string stackId, string arenaId) =>
            new Dictionary<string, string>
            {
                [LanBeaconCodec.STACK_METADATA_KEY] = stackId,
                [ArenaDefinition.METADATA_KEY] = arenaId,
            };
    }
}
