using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay
{
    /// Ходьба, прыжок и взгляд от первого лица. Компонент завёлся не от хорошей жизни:
    /// FirstPersonController из StarterAssets двигает персонажа в Update, а Fusion
    /// в Shared Mode каждый кадр заново накладывает на трансформ сетевое состояние —
    /// всё, что записано между тиками, стирается, и капсула стоит на месте. Двигаться
    /// персонаж обязан внутри тика, поэтому шаг здесь и не привязан к Update: его зовёт
    /// адаптер стека, у Fusion это FixedUpdateNetwork.
    ///
    /// Сети компонент по-прежнему не знает — ему передают только шаг времени. Значения
    /// по умолчанию сняты с FirstPersonController на Player_Base, чтобы ходьба ощущалась
    /// одинаково на всех трёх стеках.
    ///
    /// Ссылаться на сам FirstPersonController нельзя: StarterAssets живут без asmdef,
    /// то есть в Assembly-CSharp, а он ссылается на наши сборки, а не наоборот. Ввод
    /// поэтому читается прямо из project-wide actions, как это уже делает InteractionRay.
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        /// Небольшая прижимающая скорость на земле: с нулевой CharacterController
        /// теряет контакт на первом же уклоне и начинает считать себя падающим.
        private const float GroundedFallSpeed = -2f;

        [SerializeField] private Transform view;
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference lookAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference sprintAction;

        [Header("Значения FirstPersonController у Player_Base")]
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float sprintSpeed = 6f;
        [SerializeField] private float speedChangeRate = 10f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -15f;
        [SerializeField] private float lookSensitivity = 1f;
        [SerializeField] private float topClamp = 89f;
        [SerializeField] private float bottomClamp = -89f;

        private CharacterController _controller;
        private float _yaw;
        private float _pitch;
        private float _speed;
        private float _fallSpeed;
        private bool _jumpPending;

        private void Awake() => _controller = GetComponent<CharacterController>();

        /// Компонент включается в момент, когда сеть сказала «этот персонаж мой», —
        /// к этому времени капсула уже стоит в своей точке спавна, и направление взгляда
        /// надо взять оттуда, а не начинать с нуля.
        private void OnEnable()
        {
            _yaw = transform.eulerAngles.y;
            _pitch = 0f;

            /// CharacterController помнит позицию, снятую при включении, и на первом же Move
            /// возвращается к ней: объект создаётся в начале координат и только потом
            /// переезжает в точку спавна. Выключение и включение эту память сбрасывает —
            /// тем же приёмом и по той же причине лечит спавн NetworkCharacterController
            /// в самом Fusion.
            _controller.enabled = false;
            _controller.enabled = true;
        }

        /// Шаг симуляции: поворот корпуса, горизонтальная скорость, гравитация, прыжок.
        /// Время приходит снаружи — у Fusion это Runner.DeltaTime, то есть длина тика.
        public void Step(float deltaTime)
        {
            var facing = Quaternion.Euler(0f, _yaw, 0f);
            transform.rotation = facing;

            var input = moveAction.action.ReadValue<Vector2>();
            var wish = facing * new Vector3(input.x, 0f, input.y);
            if (wish.sqrMagnitude > 1f) wish.Normalize();

            var target = input == Vector2.zero
                ? 0f
                : sprintAction.action.IsPressed() ? sprintSpeed : moveSpeed;

            _speed = Mathf.Lerp(_speed, target, deltaTime * speedChangeRate);

            if (_controller.isGrounded && _fallSpeed < 0f)
            {
                _fallSpeed = GroundedFallSpeed;
            }

            /// Нажатие копится кадрами, а тратится тиком: тик реже кадра, и без защёлки
            /// короткое нажатие пробела попадало бы между тиками и пропадало.
            if (_jumpPending && _controller.isGrounded)
            {
                _fallSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            _jumpPending = false;
            _fallSpeed += gravity * deltaTime;

            _controller.Move((wish * _speed + Vector3.up * _fallSpeed) * deltaTime);
        }

        private void Update()
        {
            /// Мышь отдаёт смещение за кадр, а стик геймпада — постоянное отклонение,
            /// и его приходится умножать на время кадра, иначе взгляд улетает. StarterAssets
            /// различает их по схеме управления PlayerInput, у нас схемы нет — спрашиваем
            /// устройство, с которого пришло само действие.
            var scale = lookAction.action.activeControl?.device is Gamepad ? Time.deltaTime : 1f;
            var look = lookAction.action.ReadValue<Vector2>() * (lookSensitivity * scale);

            _yaw += look.x;
            _pitch = Mathf.Clamp(_pitch - look.y, bottomClamp, topClamp);

            if (jumpAction.action.WasPressedThisFrame()) _jumpPending = true;
        }

        /// Взгляд обновляется кадром, а корпус — тиком, и это не небрежность: тик у Fusion
        /// 30 Гц, и поворот головы на такой частоте читается как подтормаживание мыши.
        /// Камера висит на дочернем объекте, сетевое состояние её не трогает, поэтому
        /// смотреть можно сразу, а корпус подтягивается следующим тиком.
        ///
        /// Именно LateUpdate, а не Update: мировой поворот дочернего объекта считается
        /// от родителя, а родителя в этом же кадре ещё двигает Fusion — вся его работа,
        /// включая интерполяцию, происходит внутри NetworkRunner.Update.
        private void LateUpdate()
        {
            if (view != null) view.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
    }
}
