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
        [SerializeField] private Camera _view;
        [SerializeField] private LayerMask _interactable;
        [SerializeField] private InputActionReference _interactAction;

        private readonly Subject<Collider> _pickRequested = new();
        private readonly Subject<Unit> _dropRequested = new();

        private PlayerRig _rig;

        public Observable<Collider> PickRequested => _pickRequested;
        public Observable<Unit> DropRequested => _dropRequested;

        /// Ставится адаптером при смене держателя.
        public bool HandsBusy { get; set; }

        public Transform View => _view.transform;

        private void Awake() => _rig = GetComponent<PlayerRig>();

        private void OnEnable() => _interactAction.action.performed += OnInteractPerformed;

        private void OnDisable() => _interactAction.action.performed -= OnInteractPerformed;

        private void OnDestroy()
        {
            _pickRequested.Dispose();
            _dropRequested.Dispose();
        }

        /// Имя не случайное: PlayerInput на этом же объекте работает в режиме Send Messages
        /// и на каждое действие рассылает сообщение «On + имя действия». Обработчик с именем
        /// OnInteract Unity нашла бы по имени, не подобрала бы сигнатуру под InputValue
        /// и бросала бы MissingMethodException на каждое нажатие.
        private void OnInteractPerformed(InputAction.CallbackContext _)
        {
            if (!_rig.IsLocal) return;

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
