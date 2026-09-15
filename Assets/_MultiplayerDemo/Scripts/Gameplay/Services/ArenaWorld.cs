using System;
using System.Collections.Generic;
using Game.Core;
using Game.Net;
using UnityEngine;

namespace Game.Gameplay
{
    /// Мир арены глазами сети (спека § 5.9). Собирается ArenaScope из своей сцены и уходит
    /// сессии параметром.
    public sealed class ArenaWorld : INetWorld
    {
        private readonly ArenaSpawnPoints _points;

        public IEntityFactory Factory { get; }
        public IReadOnlyList<NetEntity> SceneEntities { get; } = Array.Empty<NetEntity>();
        public IReadOnlyList<Placement> Placements { get; } = Array.Empty<Placement>();

        public ArenaWorld(ArenaSpawnPoints points, IEntityFactory factory)
        {
            _points = points;
            Factory = factory;
        }

        public Pose AvatarPose(PlayerId player)
        {
            var point = SpawnPolicy.For(player, _points.Players);
            return new Pose(point.Position, point.Rotation);
        }
    }
}
