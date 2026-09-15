using Game.Core;
using Game.Net;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public sealed class SpawnPolicyTests
    {
        private static readonly SpawnPoint[] POINTS =
        {
            new(Vector3.zero, Quaternion.identity),
            new(Vector3.right, Quaternion.identity),
            new(Vector3.forward, Quaternion.identity),
        };

        /// У Fusion аватар себе ставит каждый сам — ответ обязан совпасть на любой машине.
        [Test]
        public void SamePlayerSamePointEverywhere() =>
            Assert.AreEqual(SpawnPolicy.For(new PlayerId(4), POINTS).Position, SpawnPolicy.For(new PlayerId(4), POINTS).Position);

        [Test]
        public void FirstPlayersGetDifferentPoints()
        {
            Assert.AreNotEqual(SpawnPolicy.For(new PlayerId(0), POINTS).Position, SpawnPolicy.For(new PlayerId(1), POINTS).Position);
            Assert.AreNotEqual(SpawnPolicy.For(new PlayerId(1), POINTS).Position, SpawnPolicy.For(new PlayerId(2), POINTS).Position);
        }

        [Test]
        public void NoPointsMeansOrigin() =>
            Assert.AreEqual(Vector3.zero, SpawnPolicy.For(new PlayerId(0), new SpawnPoint[0]).Position);
    }
}
