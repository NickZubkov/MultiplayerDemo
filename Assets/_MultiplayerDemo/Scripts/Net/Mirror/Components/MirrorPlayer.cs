using System;
using Game.Core;
using Game.Gameplay;
using Mirror;
using R3;
using UnityEngine;
using VContainer;

namespace Game.Net.Mirror
{
    /// Вся связь игрового слоя с Mirror: владение — в PlayerRig, желания игрока —
    /// в команды предмету. Решение остаётся за сервером, локально не предсказываем
    /// ничего (решение D6 спеки) — ровно как в NgoPlayer.
    ///
    /// isLocalPlayer к моменту OnStartClient уже выставлен: Mirror ставит флаги раньше
    /// коллбэков (NetworkClient.InitializeIdentityFlags), и в host-режиме тоже — объект
    /// хоста проходит через тот же BootstrapIdentity из OnHostClientSpawn.
    [RequireComponent(typeof(PlayerRig))]
    public sealed class MirrorPlayer : NetworkBehaviour
    {
        [SerializeField] private InteractionRay _ray;
        [SerializeField] private HoldPoint _holdPoint;

        private PlayerRig _rig;
        private DemoConfig _config;
        private MirrorItem _held;
        private IDisposable _subscriptions;

        public bool HandsBusy => _held != null;

        /// Куда предмет встаёт в руке. У Mirror нет ограничения NGO «родителем может быть
        /// только сетевой объект», поэтому предмет цепляется прямо к точке крепления
        /// и никаких пересчётов смещения не нужно.
        public Transform HoldAnchor => _holdPoint == null ? transform : _holdPoint.transform;

        [Inject]
        public void Construct(DemoConfig config) => _config = config;

        private void Awake() => _rig = GetComponent<PlayerRig>();

        /// Без SetLocal камера, ввод и контроллер остаются выключенными у всех капсул:
        /// PlayerRig ставит себе это состояние в Awake и ждёт, что сеть его поправит.
        public override void OnStartClient()
        {
            _rig.SetLocal(isLocalPlayer);

            if (!isLocalPlayer || _ray == null) return;

            _subscriptions = Disposable.Combine(
                _ray.PickRequested.Subscribe(OnPickRequested),
                _ray.DropRequested.Subscribe(_ => OnDropRequested()));
        }

        public override void OnStopClient() => _subscriptions?.Dispose();

        /// Ставится предметом при смене держателя — и на сервере, и у клиентов.
        public void SetHeldItem(MirrorItem item)
        {
            _held = item;

            if (_ray != null)
            {
                _ray.HandsBusy = item != null;
            }
        }

        /// Коллайдер мог оказаться на дочернем объекте — предмет ищем вверх по иерархии.
        private void OnPickRequested(Collider target)
        {
            var item = target.GetComponentInParent<MirrorItem>();
            if (item == null) return;

            item.CmdRequestPick();
        }

        private void OnDropRequested()
        {
            if (_held == null) return;

            _held.CmdRequestDrop(_ray.View.forward * _config.ThrowImpulse);
        }
    }
}
