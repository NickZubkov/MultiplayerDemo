using Game.Net.Ngo;
using NUnit.Framework;

namespace Game.Tests
{
    /// Ручной ввод в лобби разбирает этот же код, что и подключение по записи маяка: строка,
    /// принятая лобби, обязана дойти до транспорта.
    public sealed class NgoAddressTests
    {
        [Test]
        public void AddressWithPortIsParsed()
        {
            Assert.IsTrue(NgoAddress.TryParse("192.168.0.2:7788", out var address, out var port));
            Assert.AreEqual("192.168.0.2", address);
            Assert.AreEqual(7788, port);
        }

        [Test]
        public void AddressWithoutPortTakesTheDefault()
        {
            Assert.IsTrue(NgoAddress.TryParse("192.168.0.2", out var address, out var port));
            Assert.AreEqual("192.168.0.2", address);
            Assert.AreEqual(NgoAddress.DEFAULT_PORT, port);
        }

        [Test]
        public void SurroundingSpacesAreIgnored()
        {
            Assert.IsTrue(NgoAddress.TryParse("  192.168.0.2:7777  ", out var address, out _));
            Assert.AreEqual("192.168.0.2", address);
        }

        [Test]
        public void EmptyInputIsRejected() => Assert.IsFalse(NgoAddress.TryParse("   ", out _, out _));

        [Test]
        public void NonNumericPortIsRejected() => Assert.IsFalse(NgoAddress.TryParse("192.168.0.2:порт", out _, out _));

        [Test]
        public void MissingAddressIsRejected() => Assert.IsFalse(NgoAddress.TryParse(":7777", out _, out _));

        [Test]
        public void TokenIsParsedBack()
        {
            Assert.IsTrue(NgoAddress.TryParse(NgoAddress.Token("10.0.0.5", 7777), out var address, out var port));
            Assert.AreEqual("10.0.0.5", address);
            Assert.AreEqual(7777, port);
        }
    }
}
