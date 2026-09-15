using System.Collections.Generic;
using Game.Core;
using Game.Net;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay
{
    /// Мир арены глазами сети (спека § 5.9). Собирается при первом запросе — после загрузки
    /// сцены, до старта сессии — и только из своей сцены (И-24).
    public sealed class ArenaWorld : INetWorld
    {
        private readonly ArenaSpawnPoints _points;
        private readonly IReadOnlyDictionary<NetEntityId, NetEntity> _sceneIndex;

        public IEntityFactory Factory { get; }
        public IReadOnlyList<NetEntity> SceneEntities { get; }
        public IReadOnlyList<Placement> Placements { get; }

        /// Размещения убираются сразу и на каждой машине: на их местах судья создаст сетевые
        /// копии, а локальные остались бы двойниками. Сценовым сущностям — инъекция из
        /// контейнера арены: их оболочкам нужны сервисы матча.
        public ArenaWorld(ArenaSpawnPoints points, IEntityFactory factory, IObjectResolver resolver, LifetimeScope scope)
        {
            _points = points;
            Factory = factory;

            var sceneEntities = new List<NetEntity>();
            var placements = new List<Placement>();

            foreach (var root in scope.gameObject.scene.GetRootGameObjects())
            {
                foreach (var entity in root.GetComponentsInChildren<NetEntity>(true))
                {
                    if (entity.Kind == NetEntityKind.Scene)
                    {
                        resolver.InjectGameObject(entity.gameObject);
                        sceneEntities.Add(entity);
                        continue;
                    }

                    var at = entity.transform;
                    placements.Add(new Placement(entity.EntityType, at.position, at.rotation));
                    Object.Destroy(entity.gameObject);
                }
            }

            _sceneIndex = SceneEntityIndex.Build(sceneEntities);
            SceneEntities = sceneEntities;
            Placements = placements;
        }

        public Pose AvatarPose(PlayerId player)
        {
            var point = SpawnPolicy.For(player, _points.Players);
            return new Pose(point.Position, point.Rotation);
        }

        public bool TryGetSceneEntity(NetEntityId id, out NetEntity entity) => _sceneIndex.TryGetValue(id, out entity);
    }
}
