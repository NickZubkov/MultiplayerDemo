using Game.Core;
using Game.Net;
using UnityEngine;
using VContainer;

namespace Game.Gameplay
{
    /// Луч не знает ни ящиков, ни дверей (Ц15): руки заняты — «бросить» тому, что в руках,
    /// иначе «взаимодействовать» с сущностью под лучом. Новая механика луч не трогает.
    /// Занятость рук — из HoldRegistry, копии чужого состояния здесь больше нет (А-5).
    [RequireComponent(typeof(PlayerRig))]
    public sealed class InteractionRay : MonoBehaviour
    {
        [SerializeField] private Camera _view;
        [SerializeField] private LayerMask _interactable;

        private PlayerRig _rig;
        private IPlayerInput _input;
        private HoldRegistry _holds;
        private NetworkSlot _slot;
        private DemoConfig _config;

        [Inject]
        public void Construct(IPlayerInput input, HoldRegistry holds, NetworkSlot slot, DemoConfig config)
        {
            _input = input;
            _holds = holds;
            _slot = slot;
            _config = config;
        }

        private void Awake() => _rig = GetComponent<PlayerRig>();

        private void Update()
        {
            if (!_rig.IsLocal || _input == null || !_input.Current.Interact) return;

            var me = _slot.Current.CurrentValue?.Session.LocalPlayer ?? PlayerId.NONE;

            /// Импульс считаем здесь: тангаж своей головы знает только владелец.
            if (_holds.TryGetHeldBy(me, out var held))
            {
                held.Send(new DropCommand(_view.transform.forward * _config.ThrowImpulse));
                return;
            }

            var ray = new Ray(_view.transform.position, _view.transform.forward);
            if (!Physics.Raycast(ray, out var hit, PickupRules.MAX_DISTANCE, _interactable)) return;

            /// Коллайдер мог оказаться на дочернем объекте — сущность ищем вверх по иерархии.
            var target = hit.collider.GetComponentInParent<NetEntity>();
            if (target == null || target.Bound.CurrentValue == null) return;

            target.Bound.CurrentValue.Send(new InteractCommand());
        }
    }
}
