using System;
using Game.Net;
using R3;
using UnityEngine;

namespace Game.Core
{
    /// Логика ящика — одна на все стеки (спека § 6.4). Решения: кто держит, можно ли взять,
    /// что делать при уходе держателя. Применение к миру — родитель, тело, импульс — делает
    /// оболочка Crate по Holder и Thrown.
    ///
    /// Команды исполняются только у авторитета — это гарантирует сеть. Гонку двоих разрешает
    /// сама очередь команд у судьи: второй найдёт ящик уже занятым.
    public sealed class CrateLogic : IDisposable
    {
        private readonly INetEntity _entity;
        private readonly IMatchPlayers _players;
        private readonly HoldRegistry _holds;
        private readonly IHudMessages _hud;
        private readonly Func<Vector3> _position;
        private readonly float _maxImpulse;
        private readonly ItemState _item = new();
        private readonly ReactiveProperty<PlayerId> _holder = new(PlayerId.NONE);
        private readonly Subject<Vector3> _thrown = new();
        private readonly IDisposable _subscriptions;

        public ReadOnlyReactiveProperty<PlayerId> Holder => _holder;

        /// Импульс броска — только у авторитета: физику свободного ящика считает он.
        public Observable<Vector3> Thrown => _thrown;

        public CrateLogic(
            INetEntity entity,
            IMatchPlayers players,
            HoldRegistry holds,
            IHudMessages hud,
            Func<Vector3> position,
            float maxImpulse)
        {
            _entity = entity;
            _players = players;
            _holds = holds;
            _hud = hud;
            _position = position;
            _maxImpulse = maxImpulse;

            _subscriptions = Disposable.Combine(
                entity.OnState<CrateState>(OnState),
                entity.On<InteractCommand>(OnInteract),
                entity.On<DropCommand>(OnDrop),
                entity.On<PickupDenied>(OnDenied),
                entity.AuthorityChanged.Subscribe(_ => Revalidate()),
                players.Left.Subscribe(OnPlayerLeft));
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            _holds.Change(_entity, _holder.Value, PlayerId.NONE);
            _holder.Dispose();
            _thrown.Dispose();
        }

        public void RequestInteract() => _entity.Send(new InteractCommand());

        public void RequestDrop(Vector3 impulse) => _entity.Send(new DropCommand(impulse));

        private void OnInteract(PlayerId sender, InteractCommand _)
        {
            var denial = PickupRules.Evaluate(QueryFor(sender));

            if (denial == PickupDenial.None && _item.TryHold(sender))
            {
                _entity.SetState(new CrateState(sender));
                return;
            }

            /// Отказ без причины возможен только если автомат уже отдал ящик: две команды
            /// судья разбирает по очереди, второй — «занято».
            var reason = denial == PickupDenial.None ? PickupDenial.ItemHeld : denial;
            _entity.Notify(sender, new PickupDenied(reason));
        }

        private void OnDrop(PlayerId sender, DropCommand command)
        {
            if (!_item.TryRelease(sender)) return;

            _entity.SetState(new CrateState(PlayerId.NONE));
            _thrown.OnNext(Vector3.ClampMagnitude(command.Impulse, _maxImpulse));
        }

        private void OnDenied(PlayerId _, PickupDenied message) => _hud.Show(PickupDenialText.Describe(message.Reason));

        /// Состояние приходит на все машины, у авторитета — тоже, сразу из SetState.
        private void OnState(CrateState state)
        {
            var previous = _holder.Value;
            _item.Restore(state.Holder);
            _holds.Change(_entity, previous, state.Holder);
            _holder.Value = state.Holder;
        }

        private void OnPlayerLeft(PlayerId player)
        {
            if (_entity.IsAuthority && _holder.Value == player) Release();
        }

        /// Новый судья (сценарий Fusion) перепроверяет держателя: тот мог уйти вместе со старым.
        private void Revalidate()
        {
            if (!_entity.IsAuthority || _holder.Value.IsNone || _players.IsPresent(_holder.Value)) return;

            Release();
        }

        private void Release() => _entity.SetState(new CrateState(PlayerId.NONE));

        /// Луч уже проверен на машине просящего; авторитету хватает дистанции.
        private PickupQuery QueryFor(PlayerId sender)
        {
            var distance = _players.TryGetPosition(sender, out var at)
                ? Vector3.Distance(at, _position())
                : float.MaxValue;

            return new PickupQuery(
                itemFree: _item.Phase == ItemPhase.Free,
                handsEmpty: !_holds.IsHolding(sender),
                distance: distance,
                hasLineOfSight: true);
        }
    }
}
