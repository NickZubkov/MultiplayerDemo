using System;
using Fusion;
using Game.Core;
using Game.Gameplay;
using R3;
using UnityEngine;
using VContainer;

namespace Game.Net.Fusion
{
    /// Единственный факт, который сеть обязана сообщить игровому слою: «этот персонаж мой».
    /// Без него PlayerRig остаётся в состоянии из своего Awake — камера, ввод и движение
    /// выключены у всех капсул сразу.
    ///
    /// «Мой» в Shared Mode — это власть над состоянием: аватар создаёт себе сам владелец,
    /// и StateAuthority остаётся у него. Ни владельца ввода, ни сервера спрашивать не нужно,
    /// и желания игрока никуда не отправляются: тот, кто их придумал, их же и исполняет.
    ///
    /// Отсюда же и движение. FirstPersonController на этом префабе выключен, а персонажа
    /// двигает PlayerMotor — но не сам, а отсюда, из FixedUpdateNetwork: NetworkTransform
    /// в Shared Mode накладывает состояние на трансформ владельца каждый кадр, и всё,
    /// что записано мимо тика, стирается вместе с движением.
    [RequireComponent(typeof(PlayerRig))]
    public sealed class FusionPlayer : NetworkBehaviour
    {
        [SerializeField] private InteractionRay ray;
        [SerializeField] private HoldPoint holdPoint;
        [SerializeField] private PlayerMotor motor;

        private PlayerRig _rig;
        private DemoConfig _config;
        private FusionItem _held;
        private IDisposable _subscriptions;

        public bool HandsBusy => _held != null;

        /// Куда предмет встаёт в руке. Родителем предмет не цепляется: сетевое состояние
        /// трансформа у Fusion мировое, и смена родителя разошлась бы с ним — предмет
        /// каждый тик дёргало бы между рукой и последней принятой точкой.
        public Transform HoldAnchor => holdPoint == null ? transform : holdPoint.transform;

        [Inject]
        public void Construct(DemoConfig config) => _config = config;

        private void Awake() => _rig = GetComponent<PlayerRig>();

        public override void Spawned()
        {
            _rig.SetLocal(HasStateAuthority);

            if (!HasStateAuthority || ray == null) return;

            _subscriptions = Disposable.Combine(
                ray.PickRequested.Subscribe(OnPickRequested),
                ray.DropRequested.Subscribe(_ => OnDropRequested()));
        }

        public override void Despawned(NetworkRunner runner, bool hasState) => _subscriptions?.Dispose();

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || motor == null) return;

            motor.Step(Runner.DeltaTime);
        }

        /// Ставится предметом при смене держателя — и у владельца, и у чужих машин.
        public void SetHeldItem(FusionItem item)
        {
            _held = item;

            if (ray != null)
            {
                ray.HandsBusy = item != null;
            }
        }

        /// Коллайдер мог оказаться на дочернем объекте — предмет ищем вверх по иерархии.
        private void OnPickRequested(Collider target)
        {
            var item = target.GetComponentInParent<FusionItem>();
            if (item == null) return;

            item.TryTake(this);
        }

        private void OnDropRequested()
        {
            if (_held == null) return;

            _held.Release(ray.View.forward * _config.ThrowImpulse);
        }
    }
}
