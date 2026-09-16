using System.Collections.Generic;
using Game.Net;
using NUnit.Framework;
using R3;

namespace Game.Tests
{
    /// Реестр каждый кадр отдаёт новый список, и лобби перерисовывается на каждое сообщение
    /// потока: цена ошибки здесь — шестьдесят перерисовок в секунду или замерший список.
    public sealed class HostListPublisherTests
    {
        [Test]
        public void FirstListReachesSubscriber()
        {
            var publisher = new HostListPublisher();
            var received = Watch(publisher);

            publisher.Publish(List(Host("192.168.0.2:7777")));

            Assert.AreEqual(1, received.Count);
            Assert.AreEqual(1, received[0].Count);
            publisher.Dispose();
        }

        [Test]
        public void SameContentIsNotPublishedTwice()
        {
            var publisher = new HostListPublisher();
            publisher.Publish(List(Host("192.168.0.2:7777")));
            var received = Watch(publisher);

            publisher.Publish(List(Host("192.168.0.2:7777")));

            Assert.AreEqual(0, received.Count);
            publisher.Dispose();
        }

        [Test]
        public void PlayerCountChangeIsPublished()
        {
            var publisher = new HostListPublisher();
            publisher.Publish(List(Host("192.168.0.2:7777", players: 1)));
            var received = Watch(publisher);

            publisher.Publish(List(Host("192.168.0.2:7777", players: 2)));

            Assert.AreEqual(1, received.Count);
            publisher.Dispose();
        }

        /// Уровень хоста игрок видит в строке списка: смена уровня между матчами обязана
        /// до неё дойти, а метаданные — единственное, чем она отличается.
        [Test]
        public void MetadataChangeIsPublished()
        {
            var publisher = new HostListPublisher();
            publisher.Publish(List(Host("192.168.0.2:7777", arenaId: "box")));
            var received = Watch(publisher);

            publisher.Publish(List(Host("192.168.0.2:7777", arenaId: "yard")));

            Assert.AreEqual(1, received.Count);
            publisher.Dispose();
        }

        /// Длина не меняется, когда один хост сменился другим в том же кадре, — сравнение
        /// по длине этого не поймало бы.
        [Test]
        public void OneHostReplacedByAnotherIsPublished()
        {
            var publisher = new HostListPublisher();
            publisher.Publish(List(Host("192.168.0.2:7777")));
            var received = Watch(publisher);

            publisher.Publish(List(Host("192.168.0.3:7777")));

            Assert.AreEqual(1, received.Count);
            publisher.Dispose();
        }

        [Test]
        public void DisappearedHostIsPublished()
        {
            var publisher = new HostListPublisher();
            publisher.Publish(List(Host("192.168.0.2:7777")));
            var received = Watch(publisher);

            publisher.Publish(List());

            Assert.AreEqual(1, received.Count);
            publisher.Dispose();
        }

        /// Подписка на свойство сразу отдаёт текущее значение — его в счёт не берём: считаем
        /// только то, что публикатор решил разослать после.
        private static List<IReadOnlyList<HostEntry>> Watch(HostListPublisher publisher)
        {
            var received = new List<IReadOnlyList<HostEntry>>();
            publisher.Hosts.Skip(1).Subscribe(received.Add);
            return received;
        }

        private static IReadOnlyList<HostEntry> List(params HostEntry[] hosts) => hosts;

        private static HostEntry Host(string token, int players = 1, string arenaId = "box") =>
            new HostEntry("Коля", players, 4, token, new Dictionary<string, string> { ["arena"] = arenaId });
    }
}
