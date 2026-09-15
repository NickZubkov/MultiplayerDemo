using System;
using System.Collections.Generic;
using Game.Net;
using Game.Net.Local;
using NUnit.Framework;
using R3;

namespace Game.Tests
{
    /// Две-три «машины» в одном процессе: так проверяется то, что руками на одной машине
    /// не воспроизводится вовсе.
    public sealed class NetEntityChannelTests
    {
        private static readonly NetEntityId DOOR = NetEntityId.Scene("door");

        private struct Counter : INetMessage
        {
            public int Value;

            public Counter(int value)
            {
                Value = value;
            }

            public void Write(ref NetWriter writer) => writer.WriteInt(Value);

            public void Read(ref NetReader reader) => Value = reader.ReadInt();
        }

        [Test]
        public void StateReachesOtherMachineOnPump()
        {
            var net = new LocalNetwork();
            var judge = net.Join().CreateEntity(DOOR, PlayerId.NONE);
            var other = net.Join().CreateEntity(DOOR, PlayerId.NONE);
            var seen = new List<int>();
            other.OnState<Counter>(state => seen.Add(state.Value));

            judge.SetState(new Counter(5));
            CollectionAssert.IsEmpty(seen);

            net.Pump();
            CollectionAssert.AreEqual(new[] { 5 }, seen);
        }

        [Test]
        public void LateEntityGetsCurrentState()
        {
            var net = new LocalNetwork();
            var judge = net.Join().CreateEntity(DOOR, PlayerId.NONE);
            judge.SetState(new Counter(7));
            net.Pump();

            var late = net.Join().CreateEntity(DOOR, PlayerId.NONE);
            var seen = 0;
            late.OnState<Counter>(state => seen = state.Value);
            net.Pump();

            Assert.AreEqual(7, seen);
        }

        [Test]
        public void CommandReachesAuthorityWithSender()
        {
            var net = new LocalNetwork();
            var judge = net.Join().CreateEntity(DOOR, PlayerId.NONE);
            var client = net.Join();
            var clientDoor = client.CreateEntity(DOOR, PlayerId.NONE);
            var sender = PlayerId.NONE;
            judge.On<Counter>((from, _) => sender = from);

            clientDoor.Send(new Counter(1));
            net.Pump();

            Assert.AreEqual(client.Player, sender);
        }

        [Test]
        public void NotifyReachesOnlyTarget()
        {
            var net = new LocalNetwork();
            var judge = net.Join().CreateEntity(DOOR, PlayerId.NONE);
            var target = net.Join();
            var targetDoor = target.CreateEntity(DOOR, PlayerId.NONE);
            var bystanderDoor = net.Join().CreateEntity(DOOR, PlayerId.NONE);
            var hits = new List<string>();
            targetDoor.On<Counter>((_, _) => hits.Add("target"));
            bystanderDoor.On<Counter>((_, _) => hits.Add("bystander"));

            judge.Notify(target.Player, new Counter(1));
            net.Pump();

            CollectionAssert.AreEqual(new[] { "target" }, hits);
        }

        [Test]
        public void OnlyAuthorityWritesState()
        {
            var net = new LocalNetwork();
            net.Join().CreateEntity(DOOR, PlayerId.NONE);
            var client = net.Join().CreateEntity(DOOR, PlayerId.NONE);

            Assert.Throws<InvalidOperationException>(() => client.SetState(new Counter(1)));
        }

        [Test]
        public void SecondHandlerForSameTypeThrows()
        {
            var net = new LocalNetwork();
            var door = net.Join().CreateEntity(DOOR, PlayerId.NONE);
            door.On<Counter>((_, _) => { });

            Assert.Throws<InvalidOperationException>(() => door.On<Counter>((_, _) => { }));
        }

        /// Сценарий Fusion: мастер-клиент ушёл, власть над миром переходит к новому.
        [Test]
        public void JudgeLeavingMovesAuthority()
        {
            var net = new LocalNetwork();
            var first = net.Join();
            first.CreateEntity(DOOR, PlayerId.NONE);
            var second = net.Join();
            var secondDoor = second.CreateEntity(DOOR, PlayerId.NONE);
            var changed = false;
            secondDoor.AuthorityChanged.Subscribe(_ => changed = true);

            net.Leave(first);

            Assert.IsTrue(secondDoor.IsAuthority);
            Assert.IsTrue(changed);
        }
    }
}
