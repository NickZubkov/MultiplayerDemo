using Game.Net.Mirror;
using NUnit.Framework;

namespace Game.Tests
{
    /// Ручной ввод в лобби разбирает этот же код, что и подключение по записи маяка: строка,
    /// принятая лобби, обязана дойти до транспорта. Порт по умолчанию приходит параметром —
    /// своей константы у стека нет, он живёт на транспорте в сцене (И-16).
    public sealed class MirrorAddressTests
    {
        private const ushort OWN_PORT = 7778;

        [Test]
        public void AddressWithPortIsParsed()
        {
            Assert.IsTrue(MirrorAddress.TryParse("192.168.0.2:7788", OWN_PORT, out var address, out var port));
            Assert.AreEqual("192.168.0.2", address);
            Assert.AreEqual(7788, port);
        }

        [Test]
        public void AddressWithoutPortTakesTheFallback()
        {
            Assert.IsTrue(MirrorAddress.TryParse("192.168.0.2", OWN_PORT, out var address, out var port));
            Assert.AreEqual("192.168.0.2", address);
            Assert.AreEqual(OWN_PORT, port);
        }

        [Test]
        public void SurroundingSpacesAreIgnored()
        {
            Assert.IsTrue(MirrorAddress.TryParse("  192.168.0.2:7778  ", OWN_PORT, out var address, out _));
            Assert.AreEqual("192.168.0.2", address);
        }

        [Test]
        public void EmptyInputIsRejected() => Assert.IsFalse(MirrorAddress.TryParse("   ", OWN_PORT, out _, out _));

        [Test]
        public void NonNumericPortIsRejected() =>
            Assert.IsFalse(MirrorAddress.TryParse("192.168.0.2:порт", OWN_PORT, out _, out _));

        [Test]
        public void MissingAddressIsRejected() =>
            Assert.IsFalse(MirrorAddress.TryParse(":7778", OWN_PORT, out _, out _));

        [Test]
        public void TokenIsParsedBack()
        {
            var token = MirrorAddress.Token("10.0.0.5", 7778);

            Assert.IsTrue(MirrorAddress.TryParse(token, OWN_PORT, out var address, out var port));
            Assert.AreEqual("10.0.0.5", address);
            Assert.AreEqual(7778, port);
        }
    }
}
