using System;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public sealed class SpeedGuardTests
    {
        private const float Tick = 1f / 30f;

        [Test]
        public void NormalStepIsPlausible() =>
            Assert.IsTrue(SpeedGuard.IsPlausible(5f * Tick, Tick, 5f, SpeedGuard.DefaultTolerance));

        [Test]
        public void SprintBurstWithinToleranceIsPlausible() =>
            Assert.IsTrue(SpeedGuard.IsPlausible(5f * Tick * 1.4f, Tick, 5f, SpeedGuard.DefaultTolerance));

        [Test]
        public void TeleportIsRejected() =>
            Assert.IsFalse(SpeedGuard.IsPlausible(50f, Tick, 5f, SpeedGuard.DefaultTolerance));

        [Test]
        public void StandingStillWithZeroDeltaIsPlausible() =>
            Assert.IsTrue(SpeedGuard.IsPlausible(0f, 0f, 5f, SpeedGuard.DefaultTolerance));

        [Test]
        public void TinyJitterAtZeroDeltaIsForgiven() =>
            Assert.IsTrue(SpeedGuard.IsPlausible(0.02f, 0f, 5f, SpeedGuard.DefaultTolerance));

        /// Отрицательная дистанция — не подозрительный игрок, а ошибка вызывающего кода:
        /// расстояние такой не бывает. Поэтому исключение, а не false.
        [Test]
        public void NegativeDistanceThrows() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => SpeedGuard.IsPlausible(-1f, Tick, 5f));

        /// Четвёртый аргумент опущен намеренно: во всех тестах выше запас передаётся явно,
        /// и без этого значение по умолчанию не проверялось бы ничем. Две границы разом
        /// прижимают его к 1.5: рывок в 1.4 нормы проходит, бросок в две нормы уже нет.
        [Test]
        public void DefaultToleranceAppliesWhenArgumentOmitted()
        {
            Assert.IsTrue(SpeedGuard.IsPlausible(5f * Tick * 1.4f, Tick, 5f));
            Assert.IsFalse(SpeedGuard.IsPlausible(5f * Tick * 2f, Tick, 5f));
        }
    }
}
