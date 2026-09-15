using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Net;
using R3;
using UnityEngine;

namespace Game.Gameplay
{
    /// Игрок → его аватар на этой машине. Отсюда логика механик узнаёт, где игрок, а ящик —
    /// куда встать в руке держателя. Кто в сессии, говорит сама сессия: у каждого стека свой
    /// способ, контракт один (спека § 5.2).
    public sealed class AvatarRegistry : IMatchPlayers
    {
        private readonly Dictionary<PlayerId, PlayerAvatar> _avatars = new();
        private readonly INetSession _session;

        public IEnumerable<PlayerAvatar> Avatars => _avatars.Values;
        public Observable<PlayerId> Left => _session.PlayerLeft;

        /// Арена грузится внутри матча, когда стек уже в гнезде.
        public AvatarRegistry(NetworkSlot slot)
        {
            _session = slot.Current.CurrentValue.Session;
        }

        public void Register(PlayerAvatar avatar) => _avatars[avatar.Owner] = avatar;

        public void Unregister(PlayerAvatar avatar)
        {
            if (_avatars.TryGetValue(avatar.Owner, out var current) && current == avatar) _avatars.Remove(avatar.Owner);
        }

        public bool TryGet(PlayerId player, out PlayerAvatar avatar) => _avatars.TryGetValue(player, out avatar);

        public bool IsPresent(PlayerId player) => _session.Players.Contains(player);

        public bool TryGetPosition(PlayerId player, out Vector3 position)
        {
            if (_avatars.TryGetValue(player, out var avatar))
            {
                position = avatar.transform.position;
                return true;
            }

            position = default;
            return false;
        }
    }
}
