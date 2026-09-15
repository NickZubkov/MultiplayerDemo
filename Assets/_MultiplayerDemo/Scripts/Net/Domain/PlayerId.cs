using System;

namespace Game.Net
{
    /// Игрок сессии, одинаковый на всех машинах. У NGO нулевой clientId — это хост,
    /// законный держатель предмета, поэтому «никто» не может быть нулём (И-1). И не может
    /// быть отдельным значением, которое надо не забыть присвоить: default(PlayerId) —
    /// поле незаполненного сообщения, свежей структуры — обязан значить «никто». Отсюда
    /// хранение со сдвигом на единицу.
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public static readonly PlayerId NONE = default;

        private readonly int _shifted;

        public int Value => _shifted - 1;
        public bool IsNone => _shifted <= 0;

        public PlayerId(int value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Игрок не бывает отрицательным");

            _shifted = value + 1;
        }

        public static bool operator ==(PlayerId left, PlayerId right) => left._shifted == right._shifted;

        public static bool operator !=(PlayerId left, PlayerId right) => left._shifted != right._shifted;

        public bool Equals(PlayerId other) => _shifted == other._shifted;

        public override bool Equals(object obj) => obj is PlayerId other && Equals(other);

        public override int GetHashCode() => _shifted;

        public override string ToString() => IsNone ? "нет" : Value.ToString();
    }
}
