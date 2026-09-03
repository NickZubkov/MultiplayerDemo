using System;
using Game.Core;
using Game.Gameplay;
using R3;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Game.Net.Ngo
{
    /// Вся связь игрового слоя с NGO: владение — в PlayerRig, желания игрока —
    /// в запросы к предмету. Решение остаётся за сервером, локально не предсказываем
    /// ничего (решение D6 спеки).
    [RequireComponent(typeof(PlayerRig))]
    public sealed class NgoPlayer : NetworkBehaviour
    {
        [SerializeField] private InteractionRay ray;
        [SerializeField] private HoldPoint holdPoint;

        private PlayerRig _rig;
        private DemoConfig _config;
        private NgoItem _held;
        private IDisposable _subscriptions;

        public bool HandsBusy => _held != null;

        /// Точка крепления в системе координат самого игрока: предмет цепляется
        /// к корню, потому что родителем в NGO может быть только объект с NetworkObject.
        public Vector3 HoldLocalPosition =>
            holdPoint == null ? Vector3.zero : transform.InverseTransformPoint(holdPoint.transform.position);

        public Quaternion HoldLocalRotation =>
            holdPoint == null ? Quaternion.identity : Quaternion.Inverse(transform.rotation) * holdPoint.transform.rotation;

        [Inject]
        public void Construct(DemoConfig config) => _config = config;

        private void Awake() => _rig = GetComponent<PlayerRig>();

        public override void OnNetworkSpawn()
        {
            _rig.SetLocal(IsOwner);

            if (!IsOwner || ray == null) return;

            _subscriptions = Disposable.Combine(
                ray.PickRequested.Subscribe(OnPickRequested),
                ray.DropRequested.Subscribe(_ => OnDropRequested()));
        }

        public override void OnNetworkDespawn() => _subscriptions?.Dispose();

        /// Ставится предметом при смене держателя — и на сервере, и у клиентов.
        public void SetHeldItem(NgoItem item)
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
            var item = target.GetComponentInParent<NgoItem>();
            if (item == null) return;

            item.RequestPickRpc();
        }

        private void OnDropRequested()
        {
            if (_held == null) return;

            _held.RequestDropRpc(ray.View.forward * _config.ThrowImpulse);
        }
    }
}
