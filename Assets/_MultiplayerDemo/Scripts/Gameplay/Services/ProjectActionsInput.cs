using Game.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay
{
    /// Кадр считается один раз за кадр движка: читают его и контроллер, и луч взаимодействия,
    /// и «нажато в этом кадре» обязано означать одно и то же для обоих.
    public sealed class ProjectActionsInput : IPlayerInput
    {
        private readonly PlayerInputBindings _bindings;
        private readonly IInputGate _gate;

        private int _frame = -1;
        private PlayerInputFrame _current;

        public PlayerInputFrame Current
        {
            get
            {
                if (_frame == Time.frameCount) return _current;

                _frame = Time.frameCount;
                _current = _gate.IsBlocked ? PlayerInputFrame.EMPTY : Read();
                return _current;
            }
        }

        public ProjectActionsInput(PlayerInputBindings bindings, IInputGate gate)
        {
            _bindings = bindings;
            _gate = gate;
        }

        /// Мышь отдаёт смещение за кадр, а стик — постоянное отклонение: его приходится
        /// умножать на время кадра, иначе взгляд улетает. Устройство спрашиваем у самого
        /// действия — схемы управления PlayerInput у нас нет (перенесено из PlayerMotor).
        private PlayerInputFrame Read()
        {
            var look = _bindings.Look;
            var scale = look.activeControl?.device is Gamepad ? Time.deltaTime : 1f;

            return new PlayerInputFrame(
                _bindings.Move.ReadValue<Vector2>(),
                look.ReadValue<Vector2>() * (_bindings.LookSensitivity * scale),
                _bindings.Jump.WasPressedThisFrame(),
                _bindings.Sprint.IsPressed(),
                _bindings.Interact.WasPressedThisFrame());
        }
    }
}
