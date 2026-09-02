using NUnit.Framework;

namespace Game.Core.Tests
{
    public sealed class LanBeaconCodecTests
    {
        [Test]
        public void RoundTripPreservesAllFields()
        {
            var source = new HostEntry("Коля", 2, 4, "ngo", "192.168.0.2:7777");
            Assert.IsTrue(LanBeaconCodec.TryDecode(LanBeaconCodec.Encode(source), out var decoded));
            Assert.AreEqual("Коля", decoded.Name);
            Assert.AreEqual(2, decoded.Players);
            Assert.AreEqual(4, decoded.MaxPlayers);
            Assert.AreEqual("ngo", decoded.StackId);
            Assert.AreEqual("192.168.0.2:7777", decoded.JoinToken);
        }

        [Test]
        public void ForeignTrafficIsRejected() => Assert.IsFalse(LanBeaconCodec.TryDecode("какой-то мусор", out _));

        [Test]
        public void EmptyPayloadIsRejected() => Assert.IsFalse(LanBeaconCodec.TryDecode("", out _));

        [Test]
        public void TruncatedPayloadIsRejected() =>
            Assert.IsFalse(LanBeaconCodec.TryDecode(LanBeaconCodec.Magic + "|Коля", out _));

        [Test]
        public void SeparatorInNameDoesNotBreakParsing()
        {
            var source = new HostEntry("Ко|ля", 1, 4, "ngo", "192.168.0.2:7777");
            Assert.IsTrue(LanBeaconCodec.TryDecode(LanBeaconCodec.Encode(source), out var decoded));
            Assert.AreEqual("192.168.0.2:7777", decoded.JoinToken);
        }
    }
}
