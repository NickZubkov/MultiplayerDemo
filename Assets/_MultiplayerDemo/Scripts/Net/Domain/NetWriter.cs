using System;
using System.Buffers.Binary;
using UnityEngine;

namespace Game.Net
{
    /// Писатель поверх чужого буфера: сообщение собирается на стеке, без аллокаций.
    public ref struct NetWriter
    {
        private readonly Span<byte> _buffer;
        private int _position;

        public ReadOnlySpan<byte> Written => _buffer.Slice(0, _position);

        public NetWriter(Span<byte> buffer)
        {
            _buffer = buffer;
            _position = 0;
        }

        public void WriteByte(byte value)
        {
            Ensure(1);
            _buffer[_position++] = value;
        }

        public void WriteBool(bool value) => WriteByte(value ? (byte)1 : (byte)0);

        public void WriteInt(int value)
        {
            Ensure(4);
            BinaryPrimitives.WriteInt32LittleEndian(_buffer.Slice(_position), value);
            _position += 4;
        }

        public void WriteFloat(float value) => WriteInt(BitConverter.SingleToInt32Bits(value));

        public void WriteVector3(Vector3 value)
        {
            WriteFloat(value.x);
            WriteFloat(value.y);
            WriteFloat(value.z);
        }

        public void WritePlayerId(PlayerId value) => WriteInt(value.Value);

        private void Ensure(int count)
        {
            if (_position + count > _buffer.Length)
            {
                throw new InvalidOperationException($"Сообщение не влезает в {_buffer.Length} байт");
            }
        }
    }
}
