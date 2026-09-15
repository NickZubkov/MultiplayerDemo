using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.Gameplay
{
    /// Единственный факт, пересекающий границу «игра ↔ сеть»: «этот персонаж мой».
    /// Он же риг аватара: корень с CharacterController и объект взгляда — всё, что нужно
    /// провайдеру, чтобы поставить контроллер (спека § 7.2).
    public sealed class PlayerRig : MonoBehaviour
    {
        [Tooltip("Работает только у владельца: камера, AudioListener")]
        [SerializeField] private Behaviour[] _ownerOnlyBehaviours;
        [SerializeField] private GameObject[] _ownerOnlyObjects;
        [SerializeField] private Transform _view;

        private AvatarControllerProvider _provider;
        private IPlayerInput _input;
        private DemoConfig _config;

        public bool IsLocal { get; private set; }
        public Transform View => _view;
        public IAvatarController Controller { get; private set; }

        [Inject]
        public void Construct(AvatarControllerProvider provider, IPlayerInput input, DemoConfig config)
        {
            _provider = provider;
            _input = input;
            _config = config;
        }

        private void Awake() => SetLocal(false);

        public void SetLocal(bool isLocal)
        {
            IsLocal = isLocal;

            foreach (var behaviour in _ownerOnlyBehaviours)
            {
                if (behaviour) behaviour.enabled = isLocal;
            }

            foreach (var go in _ownerOnlyObjects)
            {
                if (go) go.SetActive(isLocal);
            }

            /// Контроллер ставится один раз и только своему: у чужих аватаров его нет вовсе.
            if (isLocal && Controller == null && _provider != null)
            {
                Controller = _provider.Attach(this, _input, _config);
            }
        }
    }
}
