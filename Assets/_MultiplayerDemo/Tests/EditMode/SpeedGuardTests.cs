using System;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public sealed class SpeedGuardTests
    {
        private const float TICK = 1f / 30f;

        [Test]
        public void NormalStepIsPlausible() =>
            Assert.IsTrue(SpeedGuard.IsPlausible(5f * TICK, TICK, 5f, SpeedGuard.DEFAULT_TOLERANCE));

        [Test]
        public void SprintBurstWithinToleranceIsPlausible() =>
            Assert.IsTrue(SpeedGuard.IsPlausible(5f * TICK * 1.4f, TICK, 5f, SpeedGuard.DEFAULT_TOLERANCE));

        [Test]
        public void TeleportIsRejected() =>
            Assert.IsFalse(SpeedGuard.IsPlausible(50f, TICK, 5f, SpeedGuard.DEFAULT_TOLERANCE));

        [Test]
        public void StandingStillWithZeroDeltaIsPlausible() =>
            Assert.IsTrue(SpeedGuard.IsPlausible(0f, 0f, 5f, SpeedGuard.DEFAULT_TOLERANCE));

        [Test]
        public void TinyJitterAtZeroDeltaIsForgiven() =>
            Assert.IsTrue(SpeedGuard.IsPlausible(0.02f, 0f, 5f, SpeedGuard.DEFAULT_TOLERANCE));

        /// Отрицательная дистанция — не подозрительный игрок, а ошибка вызывающего кода:
        /// расстояние такой не бывает. Поэтому исключение, а не false.
        [Test]
        public void NegativeDistanceThrows() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => SpeedGuard.IsPlausible(-1f, TICK, 5f));

        /// Четвёртый аргумент опущен намеренно: во всех тестах выше запас передаётся явно,
        /// и без этого значение по умолчанию не проверялось бы ничем. Две границы разом
        /// прижимают его к 1.5: рывок в 1.4 нормы проходит, бросок в две нормы уже нет.
        [Test]
        public void DefaultToleranceAppliesWhenArgumentOmitted()
        {
            Assert.IsTrue(SpeedGuard.IsPlausible(5f * TICK * 1.4f, TICK, 5f));
            Assert.IsFalse(SpeedGuard.IsPlausible(5f * TICK * 2f, TICK, 5f));
        }
    }
}
