using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Net;
using NUnit.Framework;
using R3;

namespace Game.Tests
{
    /// Контракт сессии (спека § 5.3) держит этот класс, а не три адаптера по отдельности.
    public sealed class SessionStatePublisherTests
    {
        /// Подписчик, который из обработчика Failed зовёт выход, не должен войти в стек
        /// посреди его же коллбэка (А-3): доставка — следующим кадром.
        [Test]
        public void ReportArrivesOnNextFrame()
        {
            var frames = new FakeFrameProvider();
            using var publisher = new SessionStatePublisher(frames);
            publisher.Begin(SessionPhase.Connecting);

            publisher.Report(new SessionState(SessionPhase.Connected));
            Assert.AreEqual(SessionPhase.Idle, publisher.State.CurrentValue.Phase);

            frames.Advance();
            Assert.AreEqual(SessionPhase.Connected, publisher.State.CurrentValue.Phase);
        }

        [Test]
        public void OrderIsKept()
        {
            var frames = new FakeFrameProvider();
            using var publisher = new SessionStatePublisher(frames);
            var seen = new List<SessionPhase>();
            using var subscription = publisher.State.Subscribe(state => seen.Add(state.Phase));

            publisher.Begin(SessionPhase.Hosting);
            publisher.Report(new SessionState(SessionPhase.Failed, "сеть"));
            frames.Advance();

            CollectionAssert.AreEqual(new[] { SessionPhase.Idle, SessionPhase.Hosting, SessionPhase.Failed }, seen);
        }

        /// Ровно та ошибка, что уже случалась в Mirror: собственный выход приходил
        /// игроку сообщением «хост отключился» (А-2, И-10).
        [Test]
        public void OwnLeaveNeverBecomesFailure()
        {
            var frames = new FakeFrameProvider();
            using var publisher = new SessionStatePublisher(frames);
            publisher.Begin(SessionPhase.Hosting);
            frames.Advance();

            publisher.End();
            publisher.Report(new SessionState(SessionPhase.Failed, "Хост недоступен или отключился"));
            frames.Advance();

            Assert.AreEqual(SessionPhase.Idle, publisher.State.CurrentValue.Phase);
        }

        [Test]
        public void ReportsOutsideSessionAreIgnored()
        {
            var frames = new FakeFrameProvider();
            using var publisher = new SessionStatePublisher(frames);

            publisher.Report(new SessionState(SessionPhase.Connected));
            frames.Advance();

            Assert.AreEqual(SessionPhase.Idle, publisher.State.CurrentValue.Phase);
        }

        /// Fusion бросал из StartGame мимо всех обработчиков, и игрок оставался в арене
        /// без сессии и без сообщения (И-11).
        [Test]
        public void ExceptionBecomesFailureWithReason()
        {
            var frames = new FakeFrameProvider();
            using var publisher = new SessionStatePublisher(frames);
            publisher.Begin(SessionPhase.Connecting);

            publisher.GuardAsync(() => throw new InvalidOperationException("No match found")).GetAwaiter().GetResult();
            frames.Advance();

            Assert.AreEqual(SessionPhase.Failed, publisher.State.CurrentValue.Phase);
            Assert.AreEqual("No match found", publisher.State.CurrentValue.Reason);
        }

        [Test]
        public void CancellationIsNotFailure()
        {
            var frames = new FakeFrameProvider();
            using var publisher = new SessionStatePublisher(frames);
            publisher.Begin(SessionPhase.Connecting);

            publisher.GuardAsync(() => throw new OperationCanceledException()).GetAwaiter().GetResult();
            frames.Advance();

            Assert.AreEqual(SessionPhase.Idle, publisher.State.CurrentValue.Phase);
        }
    }
}
