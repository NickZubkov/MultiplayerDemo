using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Net;
using Game.Net.Local;
using NUnit.Framework;
using R3;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// Local — четвёртая реализация примитивов и эталон контракта: всё, что здесь, обязаны
    /// повторить три сетевых стека.
    public sealed class LocalSessionTests
    {
        private sealed class FakeFactory : IEntityFactory
        {
            public readonly List<GameObject> Created = new();

            public GameObject Create(GameObject prefab, Vector3 position, Quaternion rotation)
            {
                var instance = UnityEngine.Object.Instantiate(prefab, position, rotation);
                Created.Add(instance);
                return instance;
            }
        }

        private sealed class FakeWorld : INetWorld
        {
            public IEntityFactory Factory { get; } = new FakeFactory();
            public IReadOnlyList<NetEntity> SceneEntities { get; set; } = Array.Empty<NetEntity>();
            public IReadOnlyList<Placement> Placements { get; set; } = Array.Empty<Placement>();

            public Pose AvatarPose(PlayerId player) => new(Vector3.up, Quaternion.identity);

            public bool TryGetSceneEntity(NetEntityId id, out NetEntity entity)
            {
                entity = null;
                return false;
            }
        }

        private readonly List<UnityEngine.Object> _created = new();

        [TearDown]
        public void Clean()
        {
            foreach (var created in _created)
            {
                UnityEngine.Object.DestroyImmediate(created);
            }

            _created.Clear();

            foreach (var entity in UnityEngine.Object.FindObjectsByType<NetEntity>(FindObjectsSortMode.None))
            {
                UnityEngine.Object.DestroyImmediate(entity.gameObject);
            }
        }

        [Test]
        public void HostCreatesOwnAvatarAtPolicyPose()
        {
            var session = Session(out _);
            var world = new FakeWorld();

            session.StartHostAsync(new SessionSettings("я", 1, null), world, CancellationToken.None).Forget();

            var avatar = ((FakeFactory)world.Factory).Created[0].GetComponent<NetEntity>();
            Assert.AreEqual(Vector3.up, avatar.transform.position);
            Assert.AreEqual(session.LocalPlayer, avatar.Bound.CurrentValue.Owner);
        }

        [Test]
        public void PlacementsBecomeWorldEntitiesOwnedByNobody()
        {
            var session = Session(out _);
            var world = new FakeWorld
            {
                Placements = new[] { new Placement("crate", Vector3.right, Quaternion.identity) },
            };

            session.StartHostAsync(new SessionSettings("я", 1, null), world, CancellationToken.None).Forget();

            /// Размещения создаются раньше аватара — ящик первый.
            var crate = ((FakeFactory)world.Factory).Created[0].GetComponent<NetEntity>();
            Assert.IsTrue(crate.Bound.CurrentValue.Owner.IsNone);
            Assert.IsTrue(crate.Bound.CurrentValue.IsAuthority);
        }

        [Test]
        public void SceneEntitiesAreBound()
        {
            var session = Session(out _);
            var door = new GameObject("Door").AddComponent<NetEntity>();
            var serialized = new SerializedObject(door);
            serialized.FindProperty("_kind").enumValueIndex = (int)NetEntityKind.Scene;
            serialized.FindProperty("_sceneId").stringValue = "door-1";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var world = new FakeWorld { SceneEntities = new[] { door } };

            session.StartHostAsync(new SessionSettings("я", 1, null), world, CancellationToken.None).Forget();

            Assert.IsNotNull(door.Bound.CurrentValue);
        }

        /// Тип без префаба в каталоге не роняет старт: сессия уходит в Failed, и текст с именем
        /// типа доезжает до игрока в HUD.
        [Test]
        public void UnknownEntityTypeFailsTheSession()
        {
            var session = Session(out var frames);
            var world = new FakeWorld
            {
                Placements = new[] { new Placement("ghost", Vector3.zero, Quaternion.identity) },
            };

            session.StartHostAsync(new SessionSettings("я", 1, null), world, CancellationToken.None).Forget();
            frames.Advance();

            Assert.AreEqual(SessionPhase.Failed, session.State.CurrentValue.Phase);
            StringAssert.Contains("ghost", session.State.CurrentValue.Reason);
        }

        private LocalSession Session(out FakeFrameProvider frames)
        {
            var catalog = ScriptableObject.CreateInstance<EntityCatalog>();
            _created.Add(catalog);
            var serialized = new SerializedObject(catalog);
            var entries = serialized.FindProperty("_entries");
            entries.arraySize = 2;
            entries.GetArrayElementAtIndex(0).FindPropertyRelative("_entityType").stringValue = EntityCatalog.AVATAR_TYPE;
            entries.GetArrayElementAtIndex(0).FindPropertyRelative("_prefab").objectReferenceValue = Prefab("player");
            entries.GetArrayElementAtIndex(1).FindPropertyRelative("_entityType").stringValue = "crate";
            entries.GetArrayElementAtIndex(1).FindPropertyRelative("_prefab").objectReferenceValue = Prefab("crate");
            serialized.ApplyModifiedPropertiesWithoutUndo();

            frames = new FakeFrameProvider();
            return new LocalSession(frames, catalog);
        }

        private GameObject Prefab(string entityType)
        {
            var prefab = new GameObject(entityType);
            var entity = prefab.AddComponent<NetEntity>();
            var serialized = new SerializedObject(entity);
            serialized.FindProperty("_entityType").stringValue = entityType;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _created.Add(prefab);
            return prefab;
        }
    }
}
