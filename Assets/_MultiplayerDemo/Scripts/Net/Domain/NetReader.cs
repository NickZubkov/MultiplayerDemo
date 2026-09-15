using System;
using System.Buffers.Binary;
using UnityEngine;

namespace Game.Net
{
    /// Зеркало NetWriter: читает из чужого буфера ровно в том порядке, в котором писали.
    public ref struct NetReader
    {
        private readonly ReadOnlySpan<byte> _buffer;
        private int _position;

        public NetReader(ReadOnlySpan<byte> buffer)
        {
            _buffer = buffer;
            _position = 0;
        }

        public byte ReadByte()
        {
            Ensure(1);
            return _buffer[_position++];
        }

        public bool ReadBool() => ReadByte() != 0;

        public int ReadInt()
        {
            Ensure(4);
            var value = BinaryPrimitives.ReadInt32LittleEndian(_buffer.Slice(_position));
            _position += 4;
            return value;
        }

        public float ReadFloat() => BitConverter.Int32BitsToSingle(ReadInt());

        /// Порядок чтения задан явными переменными, а не порядком аргументов конструктора:
        /// перестановка полей в нём молча перепутала бы оси.
        public Vector3 ReadVector3()
        {
            var x = ReadFloat();
            var y = ReadFloat();
            var z = ReadFloat();

            return new Vector3(x, y, z);
        }

        /// Писатель кладёт Value, и у «никого» это −1: отрицательное число читается обратно
        /// в PlayerId.NONE, а не падает на проверке конструктора.
        public PlayerId ReadPlayerId()
        {
            var value = ReadInt();

            return value < 0 ? PlayerId.NONE : new PlayerId(value);
        }

        private void Ensure(int count)
        {
            if (_position + count > _buffer.Length)
            {
                throw new InvalidOperationException("Сообщение короче ожидаемого");
            }
        }
    }
}
