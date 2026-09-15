using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay
{
    /// Ссылки на действия project-wide actions. Ссылкой, а не строкой пути: переименование
    /// действия в ассете видно в инспекторе, а не молчаливым null в рантайме (урок И-23).
    [CreateAssetMenu(menuName = "Демка/Привязки ввода", fileName = "PlayerInputBindings")]
    public sealed class PlayerInputBindings : ScriptableObject
    {
        [SerializeField] private InputActionReference _move;
        [SerializeField] private InputActionReference _look;
        [SerializeField] private InputActionReference _jump;
        [SerializeField] private InputActionReference _sprint;
        [SerializeField] private InputActionReference _interact;
        [SerializeField] private float _lookSensitivity = 1f;

        public InputAction Move => _move.action;
        public InputAction Look => _look.action;
        public InputAction Jump => _jump.action;
        public InputAction Sprint => _sprint.action;
        public InputAction Interact => _interact.action;
        public float LookSensitivity => _lookSensitivity;
    }
}
