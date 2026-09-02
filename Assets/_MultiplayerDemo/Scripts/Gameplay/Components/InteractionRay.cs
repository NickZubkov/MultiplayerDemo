using Game.Core;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay
{
    /// Локальный и мгновенный: луч, подсветка, нажатие. Сети не касается —
    /// только сообщает адаптеру «игрок хочет взять вот это».
    [RequireComponent(typeof(PlayerRig))]
    public sealed class InteractionRay : MonoBehaviour
    {
        [SerializeField] private Camera view;
        [SerializeField] private LayerMask interactable;
        [SerializeField] private InputActionReference interactAction;

        private readonly Subject<Collider> _pickRequested = new();
        private readonly Subject<Unit> _dropRequested = new();

        private PlayerRig _rig;

        public Observable<Collider> PickRequested => _pickRequested;
        public Observable<Unit> DropRequested => _dropRequested;

        /// Ставится адаптером при смене держателя.
        public bool HandsBusy { get; set; }

        public Transform View => view.transform;

        private void Awake() => _rig = GetComponent<PlayerRig>();

        private void OnEnable() => interactAction.action.performed += OnInteract;

        private void OnDisable() => interactAction.action.performed -= OnInteract;

        private void OnDestroy()
        {
            _pickRequested.Dispose();
            _dropRequested.Dispose();
        }

        private void OnInteract(InputAction.CallbackContext _)
        {
            if (!_rig.IsLocal) return;

            if (HandsBusy)
            {
                _dropRequested.OnNext(Unit.Default);
                return;
            }

            var ray = new Ray(view.transform.position, view.transform.forward);

            if (Physics.Raycast(ray, out var hit, PickupRules.MaxDistance, interactable))
            {
                _pickRequested.OnNext(hit.collider);
            }
        }
    }
}
