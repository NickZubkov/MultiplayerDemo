using Game.Core;
using Game.Net;
using R3;
using UnityEngine;
using VContainer;

namespace Game.Gameplay
{
    /// Оболочка двери: створка поворачивается к углу, который решила DoorLogic. Коллайдер
    /// на створке, слой Interactable — луч попадает в неё и находит сущность выше по иерархии.
    [RequireComponent(typeof(NetEntity))]
    public sealed class Door : MonoBehaviour
    {
        [SerializeField] private Transform _panel;
        [SerializeField] private float _openAngle = 90f;
        [SerializeField] private float _degreesPerSecond = 180f;

        private NetEntity _net;
        private AvatarRegistry _avatars;
        private IHudMessages _hud;
        private DoorLogic _logic;
        private DisposableBag _subscriptions;

        [Inject]
        public void Construct(AvatarRegistry avatars, IHudMessages hud)
        {
            _avatars = avatars;
            _hud = hud;
        }

        private void Awake() => _net = GetComponent<NetEntity>();

        private void Start() => _net.Bound.Where(entity => entity != null).Subscribe(OnBound).AddTo(ref _subscriptions);

        private void OnDestroy()
        {
            _subscriptions.Dispose();
            _logic?.Dispose();
        }

        private void Update()
        {
            if (_logic == null) return;

            var target = Quaternion.Euler(0f, _logic.IsOpen.CurrentValue ? _openAngle : 0f, 0f);
            _panel.localRotation = Quaternion.RotateTowards(_panel.localRotation, target, _degreesPerSecond * Time.deltaTime);
        }

        private void OnBound(INetEntity entity) =>
            _logic = new DoorLogic(entity, _avatars, _hud, () => transform.position);
    }
}
