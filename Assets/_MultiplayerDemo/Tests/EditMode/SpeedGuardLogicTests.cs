using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// Скорость меряется на окне: порог — 6 м/с × 0.5 с × 1.5 + 0.05, то есть 4.55 м за окно.
    public sealed class SpeedGuardLogicTests
    {
        private static SpeedGuardLogic Guard()
        {
            var guard = new SpeedGuardLogic(maxSpeed: 6f, tolerance: 1.5f, window: 0.5, grace: 1.0);
            guard.Reset(Vector3.zero, 0.0);
            return guard;
        }

        [Test]
        public void PlausibleStepIsAccepted() =>
            Assert.IsFalse(Guard().TryCorrect(new Vector3(2f, 0f, 0f), 0.5, out _));

        [Test]
        public void TeleportReturnsToLastAccepted()
        {
            var guard = Guard();

            Assert.IsTrue(guard.TryCorrect(new Vector3(20f, 0f, 0f), 0.5, out var returnTo));
            Assert.AreEqual(Vector3.zero, returnTo);
        }

        /// Пункт 9 чек-листа приёмки: клиенту задран множитель скорости. За окно он набирает
        /// лишние метры, и это видно, в отличие от дрожания доставки.
        [Test]
        public void SpeedHackIsCorrected() =>
            Assert.IsTrue(Guard().TryCorrect(new Vector3(9f, 0f, 0f), 0.5, out _));

        /// Поза чужого игрока приходит порциями — по снимку на тик стека, а у Fusion ещё и через
        /// облако. На шаге физики каждая порция выглядит рывком сверх порога; окно их собирает,
        /// и честный спринт (3 м за полсекунды) проходит целиком.
        [Test]
        public void DeliveryJitterInsideWindowIsNotCorrected()
        {
            var guard = Guard();

            Assert.IsFalse(guard.TryCorrect(new Vector3(1.2f, 0f, 0f), 0.2, out _));
            Assert.IsFalse(guard.TryCorrect(new Vector3(2.4f, 0f, 0f), 0.4, out _));
            Assert.IsFalse(guard.TryCorrect(new Vector3(3f, 0f, 0f), 0.5, out _));
        }

        /// Поправка идёт до владельца и обратно RTT — всё это время судья видит старую позицию.
        [Test]
        public void NoSecondCorrectionDuringGrace()
        {
            var guard = Guard();
            guard.TryCorrect(new Vector3(20f, 0f, 0f), 0.5, out _);

            Assert.IsFalse(guard.TryCorrect(new Vector3(20f, 0f, 0f), 1.2, out _));
        }

        /// И-8: вернулся и пошёл дальше — не нарушитель. Раньше база оставалась до нарушения,
        /// и после паузы игрока возвращало снова и снова.
        [Test]
        public void WalkingAfterReturnIsNotCorrectedAgain()
        {
            var guard = Guard();
            guard.TryCorrect(new Vector3(20f, 0f, 0f), 0.5, out _);

            Assert.IsFalse(guard.TryCorrect(new Vector3(2f, 0f, 0f), 1.6, out _));
            Assert.IsFalse(guard.TryCorrect(new Vector3(2.1f, 0f, 0f), 2.2, out _));
        }

        /// Проигнорировал возврат — возвращаем ещё раз, а не принимаем новую позицию молча.
        [Test]
        public void IgnoredCorrectionIsRepeated()
        {
            var guard = Guard();
            guard.TryCorrect(new Vector3(60f, 0f, 0f), 0.5, out _);

            Assert.IsTrue(guard.TryCorrect(new Vector3(60f, 0f, 0f), 1.6, out var returnTo));
            Assert.AreEqual(Vector3.zero, returnTo);
        }

        /// Вертикаль не сверяется: прыжок в беге с приземлением иначе не влез бы в порог (решение D4).
        [Test]
        public void VerticalMovementIsIgnored() =>
            Assert.IsFalse(Guard().TryCorrect(new Vector3(0f, 3f, 0f), 0.5, out _));
    }
}
