using System;
using Game.Net;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public sealed class NetMessageTests
    {
        [Test]
        public void WriterAndReaderRoundTrip()
        {
            Span<byte> buffer = stackalloc byte[NetBlob.CAPACITY];
            var writer = new NetWriter(buffer);
            writer.WriteInt(-7);
            writer.WriteBool(true);
            writer.WriteVector3(new Vector3(1.5f, -2f, 3f));
            writer.WritePlayerId(new PlayerId(2));

            var reader = new NetReader(writer.Written);

            Assert.AreEqual(-7, reader.ReadInt());
            Assert.IsTrue(reader.ReadBool());
            Assert.AreEqual(new Vector3(1.5f, -2f, 3f), reader.ReadVector3());
            Assert.AreEqual(new PlayerId(2), reader.ReadPlayerId());
        }

        /// Сообщение, не влезшее в блок, обязано падать у отправителя, а не обрезаться молча.
        [Test]
        public void OverflowThrows()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                Span<byte> buffer = stackalloc byte[4];
                var writer = new NetWriter(buffer);
                writer.WriteInt(1);
                writer.WriteByte(1);
            });
        }

        [Test]
        public void BlobKeepsBytesAndLength()
        {
            var source = new byte[] { 1, 2, 3, 250, 0, 9, 10, 11, 12, 13 };

            var blob = NetBlob.From(source);
            Span<byte> copy = stackalloc byte[NetBlob.CAPACITY];
            var length = blob.CopyTo(copy);

            Assert.AreEqual(source.Length, length);
            CollectionAssert.AreEqual(source, copy.Slice(0, length).ToArray());
        }
    }
}
