using System;
using Game.Core;
using Game.Net;
using R3;
using UnityEngine;
using VContainer;

namespace Game.Gameplay
{
    /// Оболочка аватара (спека § 6.4). Реплицируемого состояния у аватара нет: его поза идёт
    /// от владельца, а «этот персонаж мой» оболочка узнаёт из привязанной сущности.
    [RequireComponent(typeof(PlayerRig), typeof(NetEntity))]
    public sealed class PlayerAvatar : MonoBehaviour
    {
        private const string RETURNED = "Перемещение отклонено: вас вернули на место";

        [SerializeField] private HoldPoint _holdPoint;

        private PlayerRig _rig;
        private NetEntity _net;
        private AvatarRegistry _registry;
        private NetworkSlot _slot;
        private IHudMessages _hud;
        private IDisposable _bound;
        private IDisposable _correction;

        public INetEntity Entity { get; private set; }
        public PlayerId Owner => Entity?.Owner ?? PlayerId.NONE;
        public Transform HoldPoint => _holdPoint == null ? transform : _holdPoint.transform;

        [Inject]
        public void Construct(AvatarRegistry registry, NetworkSlot slot, IHudMessages hud)
        {
            _registry = registry;
            _slot = slot;
            _hud = hud;
        }

        private void Awake()
        {
            _rig = GetComponent<PlayerRig>();
            _net = GetComponent<NetEntity>();
        }

        /// Стек может привязать сущность раньше Start — Bound отдаст её сразу при подписке.
        private void Start() => _bound = _net.Bound.Where(entity => entity != null).Subscribe(OnBound);

        private void OnDestroy()
        {
            _bound?.Dispose();
            _correction?.Dispose();

            if (Entity != null) _registry.Unregister(this);
        }

        private void OnBound(INetEntity entity)
        {
            Entity = entity;
            _registry.Register(this);
            _rig.SetLocal(entity.Owner == _slot.Current.CurrentValue.Session.LocalPlayer);
            _correction = entity.On<Correction>(OnCorrection);
        }

        /// Поправка анти-телепорта приходит только владельцу: возвращается он сам и сам же
        /// видит почему (И-9). Текст — у того, кого вернули, а не у судьи.
        private void OnCorrection(PlayerId _, Correction correction)
        {
            _rig.Controller?.Teleport(correction.Position, transform.rotation);
            _hud.Show(RETURNED);
        }
    }
}
