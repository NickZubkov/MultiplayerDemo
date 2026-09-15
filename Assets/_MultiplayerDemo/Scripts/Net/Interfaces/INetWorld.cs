using System.Collections.Generic;
using UnityEngine;

namespace Game.Net
{
    /// Мир арены глазами сети (спека § 5.9): чем создавать сущности, что уже стоит в сцене,
    /// куда ставить игроков. Поза аватара детерминирована по игроку — одинакова на любой машине.
    public interface INetWorld
    {
        public IEntityFactory Factory { get; }
        public IReadOnlyList<NetEntity> SceneEntities { get; }
        public IReadOnlyList<Placement> Placements { get; }

        public Pose AvatarPose(PlayerId player);

        /// Носитель сценовой сущности, приехавший на удалённую машину, находит здесь свою копию
        /// объекта: сцену каждая машина грузит сама, а идентификатор в ней один и тот же.
        public bool TryGetSceneEntity(NetEntityId id, out NetEntity entity);
    }
}
