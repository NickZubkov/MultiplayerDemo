using System;
using Game.Net;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    public sealed class SceneEntityIndexTests
    {
        private static NetEntity Scene(string name, string sceneId)
        {
            var entity = new GameObject(name).AddComponent<NetEntity>();
            var serialized = new SerializedObject(entity);
            serialized.FindProperty("_kind").enumValueIndex = (int)NetEntityKind.Scene;
            serialized.FindProperty("_sceneId").stringValue = sceneId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return entity;
        }

        [TearDown]
        public void Clean()
        {
            foreach (var entity in UnityEngine.Object.FindObjectsByType<NetEntity>(FindObjectsSortMode.None))
            {
                UnityEngine.Object.DestroyImmediate(entity.gameObject);
            }
        }

        [Test]
        public void FindsEntityById()
        {
            var door = Scene("Door", "a1");

            var index = SceneEntityIndex.Build(new[] { door });

            Assert.AreSame(door, index[NetEntityId.Scene("a1")]);
        }

        /// Копия через Ctrl+D уносит идентификатор. Падать — с именами обоих объектов:
        /// иначе искать дубликат в сцене придётся перебором.
        [Test]
        public void DuplicateIdNamesBothObjects()
        {
            var first = Scene("Door", "a1");
            var second = Scene("Door (1)", "a1");

            var error = Assert.Throws<InvalidOperationException>(() => SceneEntityIndex.Build(new[] { first, second }));

            StringAssert.Contains("Door (1)", error.Message);
            StringAssert.Contains("Door", error.Message);
        }

        [Test]
        public void EmptyIdIsRefused() =>
            Assert.Throws<InvalidOperationException>(() => SceneEntityIndex.Build(new[] { Scene("Door", "") }));
    }
}
