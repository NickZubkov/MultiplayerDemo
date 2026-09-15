using Game.Core;
using R3;
using UnityEngine;
using VContainer;

namespace Game.Gameplay
{
    /// Локальный и мгновенный: луч, подсветка, нажатие. Сети не касается —
    /// только сообщает адаптеру «игрок хочет взять вот это».
    [RequireComponent(typeof(PlayerRig))]
    public sealed class InteractionRay : MonoBehaviour
    {
        [SerializeField] private Camera _view;
        [SerializeField] private LayerMask _interactable;

        private readonly Subject<Collider> _pickRequested = new();
        private readonly Subject<Unit> _dropRequested = new();

        private PlayerRig _rig;
        private IPlayerInput _input;

        public Observable<Collider> PickRequested => _pickRequested;
        public Observable<Unit> DropRequested => _dropRequested;

        /// Ставится адаптером при смене держателя.
        public bool HandsBusy { get; set; }

        public Transform View => _view.transform;

        [Inject]
        public void Construct(IPlayerInput input) => _input = input;

        private void Awake() => _rig = GetComponent<PlayerRig>();

        private void OnDestroy()
        {
            _pickRequested.Dispose();
            _dropRequested.Dispose();
        }

        private void Update()
        {
            if (!_rig.IsLocal || _input == null || !_input.Current.Interact) return;

            if (HandsBusy)
            {
                _dropRequested.OnNext(Unit.Default);
                return;
            }

            var ray = new Ray(_view.transform.position, _view.transform.forward);

            if (Physics.Raycast(ray, out var hit, PickupRules.MAX_DISTANCE, _interactable))
            {
                _pickRequested.OnNext(hit.collider);
            }
        }
    }
}
