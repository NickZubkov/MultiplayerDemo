using System.Collections.Generic;
using Game.Core;
using Game.Net;
using Game.Net.Local;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace Game.Tests
{
    /// Механика целиком, на нескольких «машинах» сразу. Гонка двоих — пункт 6 чек-листа,
    /// который на одной машине руками не воспроизводится вовсе.
    public sealed class CrateLogicTests
    {
        private const float MAX_IMPULSE = 5f;

        private static readonly NetEntityId CRATE_ID = NetEntityId.Scene("crate");
        private static readonly NetEntityId SECOND_CRATE_ID = NetEntityId.Scene("crate-2");

        private sealed class FakePlayers : IMatchPlayers
        {
            private readonly Subject<PlayerId> _left = new();

            public readonly Dictionary<PlayerId, Vector3> Positions = new();

            public Observable<PlayerId> Left => _left;

            public bool IsPresent(PlayerId player) => Positions.ContainsKey(player);

            public bool TryGetPosition(PlayerId player, out Vector3 position) => Positions.TryGetValue(player, out position);

            public void Leave(PlayerId player)
            {
                Positions.Remove(player);
                _left.OnNext(player);
            }
        }

        private sealed class FakeHud : IHudMessages
        {
            public string Last;

            public void Show(string message) => Last = message;
        }

        /// Одна машина сессии: своя логика ящика, свои руки, свой HUD.
        private sealed class Peer
        {
            public readonly LocalMachine Machine;
            public readonly HoldRegistry Holds = new();
            public readonly FakeHud Hud = new();
            public readonly List<Vector3> Throws = new();
            public readonly CrateLogic Crate;

            public PlayerId Player => Machine.Player;

            public Peer(LocalNetwork net, FakePlayers players)
            {
                Machine = net.Join();
                players.Positions[Machine.Player] = Vector3.one;
                Crate = CrateOf(CRATE_ID, players);
                Crate.Thrown.Subscribe(Throws.Add);
            }

            public CrateLogic CrateOf(NetEntityId id, FakePlayers players) =>
                new(Machine.CreateEntity(id, PlayerId.NONE), players, Holds, Hud, () => Vector3.zero, MAX_IMPULSE);
        }

        [Test]
        public void PickupGivesItemToRequesterEverywhere()
        {
            var net = new LocalNetwork();
            var players = new FakePlayers();
            var judge = new Peer(net, players);
            var client = new Peer(net, players);

            client.Crate.RequestInteract();
            net.Pump();

            Assert.AreEqual(client.Player, judge.Crate.Holder.CurrentValue);
            Assert.AreEqual(client.Player, client.Crate.Holder.CurrentValue);
            Assert.IsTrue(client.Holds.IsHolding(client.Player));
        }

        /// Пункт 6: двое жмут «взять» — достаётся первому, второй слышит причину.
        [Test]
        public void RaceGoesToFirstAndSecondIsToldWhy()
        {
            var net = new LocalNetwork();
            var players = new FakePlayers();
            var judge = new Peer(net, players);
            var first = new Peer(net, players);
            var second = new Peer(net, players);

            first.Crate.RequestInteract();
            second.Crate.RequestInteract();
            net.Pump();

            Assert.AreEqual(first.Player, judge.Crate.Holder.CurrentValue);
            Assert.AreEqual(PickupDenialText.Describe(PickupDenial.ItemHeld), second.Hud.Last);
            Assert.IsNull(first.Hud.Last);
        }

        [Test]
        public void TooFarIsRefused()
        {
            var net = new LocalNetwork();
            var players = new FakePlayers();
            var judge = new Peer(net, players);
            var client = new Peer(net, players);
            players.Positions[client.Player] = new Vector3(10f, 0f, 0f);

            client.Crate.RequestInteract();
            net.Pump();

            Assert.IsTrue(judge.Crate.Holder.CurrentValue.IsNone);
            Assert.AreEqual(PickupDenialText.Describe(PickupDenial.TooFar), client.Hud.Last);
        }

        [Test]
        public void BusyHandsAreRefused()
        {
            var net = new LocalNetwork();
            var players = new FakePlayers();
            var judge = new Peer(net, players);
            var client = new Peer(net, players);
            judge.CrateOf(SECOND_CRATE_ID, players);
            var secondCrate = client.CrateOf(SECOND_CRATE_ID, players);

            client.Crate.RequestInteract();
            net.Pump();
            secondCrate.RequestInteract();
            net.Pump();

            Assert.AreEqual(PickupDenialText.Describe(PickupDenial.HandsBusy), client.Hud.Last);
        }

        /// Бросок считает физику авторитет, поэтому импульс получает только он — и не сильнее конфига.
        [Test]
        public void DropReleasesEverywhereAndThrowsOnAuthorityOnly()
        {
            var net = new LocalNetwork();
            var players = new FakePlayers();
            var judge = new Peer(net, players);
            var client = new Peer(net, players);
            client.Crate.RequestInteract();
            net.Pump();

            client.Crate.RequestDrop(new Vector3(0f, 0f, 100f));
            net.Pump();

            Assert.IsTrue(client.Crate.Holder.CurrentValue.IsNone);
            Assert.IsFalse(client.Holds.IsHolding(client.Player));
            Assert.AreEqual(1, judge.Throws.Count);
            Assert.AreEqual(MAX_IMPULSE, judge.Throws[0].magnitude, 1e-4f);
            CollectionAssert.IsEmpty(client.Throws);
        }

        [Test]
        public void StrangerCannotDrop()
        {
            var net = new LocalNetwork();
            var players = new FakePlayers();
            var judge = new Peer(net, players);
            var holder = new Peer(net, players);
            var stranger = new Peer(net, players);
            holder.Crate.RequestInteract();
            net.Pump();

            stranger.Crate.RequestDrop(Vector3.forward);
            net.Pump();

            Assert.AreEqual(holder.Player, judge.Crate.Holder.CurrentValue);
        }

        /// Пункт 7: держатель ушёл — предмет падает там, где был, а не висит занятым навсегда.
        [Test]
        public void HolderLeavingDropsItem()
        {
            var net = new LocalNetwork();
            var players = new FakePlayers();
            var judge = new Peer(net, players);
            var holder = new Peer(net, players);
            holder.Crate.RequestInteract();
            net.Pump();

            net.Leave(holder.Machine);
            players.Leave(holder.Player);
            net.Pump();

            Assert.IsTrue(judge.Crate.Holder.CurrentValue.IsNone);
        }

        /// Сценарий Fusion: судья ушёл, держа предмет. Новый судья обязан это заметить сам —
        /// события ухода он мог и не увидеть, если власть пришла к нему позже.
        [Test]
        public void NewJudgeDropsItemOfDepartedHolder()
        {
            var net = new LocalNetwork();
            var players = new FakePlayers();
            var judge = new Peer(net, players);
            var next = new Peer(net, players);
            judge.Crate.RequestInteract();
            net.Pump();

            players.Positions.Remove(judge.Player);
            net.Leave(judge.Machine);
            net.Pump();

            Assert.IsTrue(next.Crate.Holder.CurrentValue.IsNone);
        }
    }
}
