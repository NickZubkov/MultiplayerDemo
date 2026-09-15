using Game.Gameplay;
using StarterAssets;
using UnityEngine;

namespace Game.Integrations
{
    /// Кормит StarterAssets нашим кадром ввода. Раньше FirstPersonController, чтобы его
    /// Update прочёл уже свежие поля.
    [DefaultExecutionOrder(-100)]
    public sealed class StarterAssetsInputFeeder : MonoBehaviour, IAvatarController
    {
        private CharacterController _body;
        private StarterAssetsInputs _inputs;
        private IPlayerInput _input;

        private void Awake() => _body = GetComponent<CharacterController>();

        public void Configure(StarterAssetsInputs inputs, IPlayerInput input)
        {
            _inputs = inputs;
            _input = input;
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            _body.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            _body.enabled = true;
        }

        private void Update()
        {
            if (_input == null) return;

            var frame = _input.Current;
            _inputs.move = frame.Move;
            _inputs.sprint = frame.Sprint;

            /// FirstPersonController умножает взгляд на время кадра, если схема управления не
            /// «KeyboardMouse», а у PlayerInput без действий схемы нет вовсе — то есть умножает
            /// всегда. Наш кадр уже в градусах за кадр, поэтому делим обратно. Знак тангажа
            /// у StarterAssets обратный: там положительный — вниз.
            var deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            _inputs.look = new Vector2(frame.Look.x, -frame.Look.y) / deltaTime;

            /// Прыжок у StarterAssets — защёлка, которую снимает сам контроллер, когда
            /// оторвался от земли; наш кадр даёт только момент нажатия.
            if (frame.Jump) _inputs.jump = true;

            /// Курсором владеем не мы: StarterAssetsInputs на каждом возврате фокуса ставит
            /// cursorLocked, и мы заранее подкладываем туда текущее состояние — его обработчик
            /// фокуса становится пустым.
            _inputs.cursorLocked = Cursor.lockState == CursorLockMode.Locked;
        }
    }
}
