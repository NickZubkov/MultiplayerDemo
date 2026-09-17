using System.Collections.Generic;
using Game.Core;
using Game.Net;
using Game.Net.Local;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace Game.Tests
{
    /// Вторая механика на той же подставке сети, что и ящик: те же машины, та же очередь.
    /// Если дверь понадобилось бы чинить в стеке — этот тест писать было бы не на чем.
    public sealed class DoorLogicTests
    {
        private static readonly NetEntityId DOOR_ID = NetEntityId.Scene("door");

        private sealed class FakePlayers : IMatchPlayers
        {
            public readonly Dictionary<PlayerId, Vector3> Positions = new();

            public Observable<PlayerId> Left => Observable.Empty<PlayerId>();

            public bool IsPresent(PlayerId player) => Positions.ContainsKey(player);

            public bool TryGetPosition(PlayerId player, out Vector3 position) => Positions.TryGetValue(player, out position);
        }

        private static DoorLogic Door(LocalMachine machine, FakePlayers players, FakeHud hud) =>
            new(machine.CreateEntity(DOOR_ID, PlayerId.NONE), players, hud, () => Vector3.zero);

        [Test]
        public void InteractTogglesDoorForEveryone()
        {
            var net = new LocalNetwork();
            var players = new FakePlayers();
            var judge = Door(net.Join(), players, new FakeHud());
            var client = net.Join();
            var clientDoor = Door(client, players, new FakeHud());
            players.Positions[client.Player] = Vector3.one;

            clientDoor.RequestInteract();
            net.Pump();

            Assert.IsTrue(judge.IsOpen.CurrentValue);
            Assert.IsTrue(clientDoor.IsOpen.CurrentValue);
        }

        [Test]
        public void TooFarIsRefused()
        {
            var net = new LocalNetwork();
            var players = new FakePlayers();
            var judge = Door(net.Join(), players, new FakeHud());
            var client = net.Join();
            var hud = new FakeHud();
            var clientDoor = Door(client, players, hud);
            players.Positions[client.Player] = new Vector3(10f, 0f, 0f);

            clientDoor.RequestInteract();
            net.Pump();

            Assert.IsFalse(judge.IsOpen.CurrentValue);
            Assert.AreEqual(PickupDenialText.Describe(PickupDenial.TooFar), hud.Last);
        }

        [Test]
        public void LateJoinerSeesOpenDoor()
        {
            var net = new LocalNetwork();
            var players = new FakePlayers();
            var judgeMachine = net.Join();
            var judge = Door(judgeMachine, players, new FakeHud());
            players.Positions[judgeMachine.Player] = Vector3.one;
            judge.RequestInteract();
            net.Pump();

            var late = Door(net.Join(), players, new FakeHud());
            net.Pump();

            Assert.IsTrue(late.IsOpen.CurrentValue);
        }
    }
}
