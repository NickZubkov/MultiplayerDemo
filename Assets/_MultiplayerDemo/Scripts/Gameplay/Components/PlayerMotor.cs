using UnityEngine;

namespace Game.Gameplay
{
    /// Контроллер по умолчанию (PlayerMotorProvider). Двигается сам, в Update: сеть только
    /// наблюдает позу владельца (спека Ц2). Ввод приходит кадром-данными — это задел под
    /// «полный Б» (спека § 13), где шаг прогоняется по чужому вводу.
    ///
    /// Ссылаться на FirstPersonController нельзя: StarterAssets живут в Assembly-CSharp.
    /// Значения по умолчанию сняты с него, чтобы ходьба не зависела от выбранного провайдера.
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour, IAvatarController
    {
        /// Небольшая прижимающая скорость на земле: с нулевой CharacterController теряет
        /// контакт на первом же уклоне и начинает считать себя падающим.
        private const float GROUNDED_FALL_SPEED = -2f;

        private CharacterController _controller;
        private Transform _view;
        private IPlayerInput _input;
        private MotorTuning _tuning;
        private float _yaw;
        private float _pitch;
        private float _speed;
        private float _fallSpeed;

        private void Awake() => _controller = GetComponent<CharacterController>();

        /// Мотор навешивается, когда капсула уже стоит в точке спавна: направление взгляда
        /// берём оттуда. CharacterController помнит позицию, снятую при включении, и на первом
        /// же Move вернулся бы к ней — выключение и включение эту память сбрасывают.
        private void OnEnable()
        {
            _yaw = transform.eulerAngles.y;
            _pitch = 0f;
            _controller.enabled = false;
            _controller.enabled = true;
        }

        public void Configure(Transform view, IPlayerInput input, MotorTuning tuning)
        {
            _view = view;
            _input = input;
            _tuning = tuning;
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            _controller.enabled = true;
            _yaw = rotation.eulerAngles.y;
            _speed = 0f;
            _fallSpeed = 0f;
        }

        private void Update()
        {
            if (_input == null) return;

            var frame = _input.Current;
            _yaw += frame.Look.x;
            _pitch = Mathf.Clamp(_pitch - frame.Look.y, _tuning.BottomClamp, _tuning.TopClamp);

            Move(frame, Time.deltaTime);
        }

        /// LateUpdate, а не Update: мировой поворот дочернего объекта считается от родителя,
        /// а родителя в этом же кадре ещё может подвинуть сеть — у Fusion вся его работа идёт
        /// внутри NetworkRunner.Update.
        private void LateUpdate()
        {
            if (_view != null) _view.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void Move(PlayerInputFrame frame, float deltaTime)
        {
            var facing = Quaternion.Euler(0f, _yaw, 0f);
            transform.rotation = facing;

            var wish = facing * new Vector3(frame.Move.x, 0f, frame.Move.y);
            if (wish.sqrMagnitude > 1f) wish.Normalize();

            var target = frame.Move == Vector2.zero
                ? 0f
                : frame.Sprint ? _tuning.SprintSpeed : _tuning.WalkSpeed;

            _speed = Mathf.Lerp(_speed, target, deltaTime * _tuning.SpeedChangeRate);

            if (_controller.isGrounded && _fallSpeed < 0f)
            {
                _fallSpeed = GROUNDED_FALL_SPEED;
            }

            if (frame.Jump && _controller.isGrounded)
            {
                _fallSpeed = Mathf.Sqrt(_tuning.JumpHeight * -2f * _tuning.Gravity);
            }

            _fallSpeed += _tuning.Gravity * deltaTime;
            _controller.Move((wish * _speed + Vector3.up * _fallSpeed) * deltaTime);
        }
    }
}
