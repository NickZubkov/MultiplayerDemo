using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// Порог: 6 м/с × Δt × 1.5 + 0.05. На шаге 0.02 с это 0.23 м.
    public sealed class SpeedGuardLogicTests
    {
        private static SpeedGuardLogic Guard()
        {
            var guard = new SpeedGuardLogic(maxSpeed: 6f, tolerance: 1.5f, grace: 0.5);
            guard.Reset(Vector3.zero, 0.0);
            return guard;
        }

        [Test]
        public void PlausibleStepIsAccepted() =>
            Assert.IsFalse(Guard().TryCorrect(new Vector3(0.1f, 0f, 0f), 0.02, out _));

        [Test]
        public void TeleportReturnsToLastAccepted()
        {
            var guard = Guard();

            Assert.IsTrue(guard.TryCorrect(new Vector3(5f, 0f, 0f), 0.02, out var returnTo));
            Assert.AreEqual(Vector3.zero, returnTo);
        }

        /// Поправка идёт до владельца и обратно RTT — всё это время сервер видит старую позицию.
        [Test]
        public void NoSecondCorrectionDuringGrace()
        {
            var guard = Guard();
            guard.TryCorrect(new Vector3(5f, 0f, 0f), 0.02, out _);

            Assert.IsFalse(guard.TryCorrect(new Vector3(5f, 0f, 0f), 0.2, out _));
        }

        /// И-8: вернулся и пошёл дальше — не нарушитель. Раньше база оставалась до нарушения,
        /// и после паузы игрока возвращало снова и снова.
        [Test]
        public void WalkingAfterReturnIsNotCorrectedAgain()
        {
            var guard = Guard();
            guard.TryCorrect(new Vector3(5f, 0f, 0f), 0.02, out _);

            Assert.IsFalse(guard.TryCorrect(new Vector3(2f, 0f, 0f), 0.6, out _));
            Assert.IsFalse(guard.TryCorrect(new Vector3(2.1f, 0f, 0f), 0.62, out _));
        }

        /// Проигнорировал возврат — возвращаем ещё раз, а не принимаем новую позицию молча.
        [Test]
        public void IgnoredCorrectionIsRepeated()
        {
            var guard = Guard();
            guard.TryCorrect(new Vector3(20f, 0f, 0f), 0.02, out _);

            Assert.IsTrue(guard.TryCorrect(new Vector3(20f, 0f, 0f), 0.6, out var returnTo));
            Assert.AreEqual(Vector3.zero, returnTo);
        }

        /// Вертикаль не сверяется: прыжок в беге с приземлением иначе не влез бы в порог (решение D4).
        [Test]
        public void VerticalMovementIsIgnored() =>
            Assert.IsFalse(Guard().TryCorrect(new Vector3(0f, 3f, 0f), 0.02, out _));
    }
}
