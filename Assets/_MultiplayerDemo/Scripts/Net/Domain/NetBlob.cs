using System;
using System.Runtime.InteropServices;

namespace Game.Net
{
    /// Всё, что носитель стека переносит за один раз: состояние сущности, тело команды или
    /// события. Восемь слов, а не массив: структура без ссылок сериализуется любым из трёх
    /// стеков и не аллоцирует на каждое сообщение. Порядок байт — little-endian: сборка
    /// одна, Standalone Windows.
    public readonly struct NetBlob
    {
        public const int CAPACITY = 64;

        public readonly ulong W0;
        public readonly ulong W1;
        public readonly ulong W2;
        public readonly ulong W3;
        public readonly ulong W4;
        public readonly ulong W5;
        public readonly ulong W6;
        public readonly ulong W7;
        public readonly byte Length;

        public NetBlob(
            ulong w0,
            ulong w1,
            ulong w2,
            ulong w3,
            ulong w4,
            ulong w5,
            ulong w6,
            ulong w7,
            byte length)
        {
            W0 = w0;
            W1 = w1;
            W2 = w2;
            W3 = w3;
            W4 = w4;
            W5 = w5;
            W6 = w6;
            W7 = w7;
            Length = length;
        }

        public static NetBlob From(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length > CAPACITY)
            {
                throw new ArgumentException($"Сообщение длиннее {CAPACITY} байт: {bytes.Length}");
            }

            Span<byte> buffer = stackalloc byte[CAPACITY];
            bytes.CopyTo(buffer);
            var words = MemoryMarshal.Cast<byte, ulong>(buffer);

            return new NetBlob(words[0], words[1], words[2], words[3], words[4], words[5], words[6], words[7],
                (byte)bytes.Length);
        }

        public int CopyTo(Span<byte> destination)
        {
            Span<ulong> words = stackalloc ulong[8];
            words[0] = W0;
            words[1] = W1;
            words[2] = W2;
            words[3] = W3;
            words[4] = W4;
            words[5] = W5;
            words[6] = W6;
            words[7] = W7;

            MemoryMarshal.AsBytes(words).Slice(0, Length).CopyTo(destination);
            return Length;
        }
    }
}
