using System;
using Game.Net;
using R3;
using UnityEngine;

namespace Game.Core
{
    /// Вторая механика — проверка того, что механика добавляется в одном месте (критерий 2
    /// спеки). Команда та же, что у ящика, — «взаимодействовать»: луч про двери не знает.
    /// Граница дистанции общая с взятием, отказ — тем же сообщением.
    public sealed class DoorLogic : IDisposable
    {
        private readonly INetEntity _entity;
        private readonly IMatchPlayers _players;
        private readonly IHudMessages _hud;
        private readonly Func<Vector3> _position;
        private readonly ReactiveProperty<bool> _open = new(false);
        private readonly IDisposable _subscriptions;

        public ReadOnlyReactiveProperty<bool> IsOpen => _open;

        public DoorLogic(INetEntity entity, IMatchPlayers players, IHudMessages hud, Func<Vector3> position)
        {
            _entity = entity;
            _players = players;
            _hud = hud;
            _position = position;

            _subscriptions = Disposable.Combine(
                entity.OnState<DoorState>(state => _open.Value = state.Open),
                entity.On<InteractCommand>(OnInteract),
                entity.On<PickupDenied>((_, denied) => _hud.Show(PickupDenialText.Describe(denied.Reason))));
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            _open.Dispose();
        }

        public void RequestInteract() => _entity.Send(new InteractCommand());

        private void OnInteract(PlayerId sender, InteractCommand _)
        {
            var near = _players.TryGetPosition(sender, out var at)
                && Vector3.Distance(at, _position()) <= PickupRules.MAX_DISTANCE;

            if (!near)
            {
                _entity.Notify(sender, new PickupDenied(PickupDenial.TooFar));
                return;
            }

            _entity.SetState(new DoorState(!_open.Value));
        }
    }
}
