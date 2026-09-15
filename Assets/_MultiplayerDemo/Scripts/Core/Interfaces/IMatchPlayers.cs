using Game.Net;
using R3;
using UnityEngine;

namespace Game.Core
{
    /// Датчики мира для логики механик: кто в матче и где он. Реализует AvatarRegistry
    /// в сцене арены (15.8), в тестах — подставка.
    public interface IMatchPlayers
    {
        public Observable<PlayerId> Left { get; }

        public bool IsPresent(PlayerId player);
        public bool TryGetPosition(PlayerId player, out Vector3 position);
    }
}
