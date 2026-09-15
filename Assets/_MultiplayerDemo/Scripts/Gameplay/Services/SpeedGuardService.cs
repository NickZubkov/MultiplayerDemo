using System.Collections.Generic;
using Game.Core;
using Game.Net;
using VContainer.Unity;

namespace Game.Gameplay
{
    /// Анти-телепорт как игровая логика (Ц14): работает у судьи и следит за всеми чужими
    /// аватарами — на всех стеках, включая Fusion, где судья — мастер-клиент.
    public sealed class SpeedGuardService : IFixedTickable
    {
        /// Поправка идёт до владельца и обратно RTT: всё это время судья видит старую позицию.
        private const double GRACE = 0.5;

        private readonly AvatarRegistry _avatars;
        private readonly NetworkSlot _slot;
        private readonly DemoConfig _config;
        private readonly IClock _clock;
        private readonly Dictionary<PlayerId, SpeedGuardLogic> _guards = new();

        public SpeedGuardService(AvatarRegistry avatars, NetworkSlot slot, DemoConfig config, IClock clock)
        {
            _avatars = avatars;
            _slot = slot;
            _config = config;
            _clock = clock;
        }

        /// Свой аватар судья не проверяет: он и так двигает его сам.
        public void FixedTick()
        {
            var session = _slot.Current.CurrentValue?.Session;
            if (session == null || !session.IsJudge) return;

            var now = _clock.Now;

            foreach (var avatar in _avatars.Avatars)
            {
                if (avatar.Entity == null || avatar.Owner == session.LocalPlayer) continue;

                if (!_guards.TryGetValue(avatar.Owner, out var guard))
                {
                    guard = new SpeedGuardLogic(_config.SprintSpeed, _config.SpeedTolerance, GRACE);
                    guard.Reset(avatar.transform.position, now);
                    _guards[avatar.Owner] = guard;
                    continue;
                }

                if (guard.TryCorrect(avatar.transform.position, now, out var returnTo))
                {
                    avatar.Entity.Notify(avatar.Owner, new Correction(returnTo));
                }
            }
        }
    }
}
