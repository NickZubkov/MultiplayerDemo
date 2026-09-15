using Game.Net;
using NUnit.Framework;

namespace Game.Tests
{
    public sealed class PlayerIdTests
    {
        /// Старый ItemState считал ноль «свободно», а у NGO ноль — это хост (И-1).
        [Test]
        public void ZeroIsARealPlayer()
        {
            Assert.IsFalse(new PlayerId(0).IsNone);
            Assert.AreNotEqual(PlayerId.NONE, new PlayerId(0));
        }

        /// Незаполненное поле сообщения не должно молча стать игроком 0.
        [Test]
        public void DefaultIsNone() => Assert.IsTrue(default(PlayerId).IsNone);

        [Test]
        public void EqualByValue() => Assert.IsTrue(new PlayerId(3) == new PlayerId(3));

        [Test]
        public void SceneAndDynamicIdsNeverCollide()
        {
            var scene = NetEntityId.Scene("5f2c");
            var dynamic = NetEntityId.Dynamic(scene.Value);

            Assert.AreNotEqual(scene, dynamic);
            Assert.IsFalse(scene.IsNone);
        }

        [Test]
        public void SceneIdIsStable() => Assert.AreEqual(NetEntityId.Scene("door-1"), NetEntityId.Scene("door-1"));
    }
}
