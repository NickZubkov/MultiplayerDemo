using Game.Core;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class PauseView : MonoBehaviour, IPauseView
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _exitButton;

        private readonly Subject<Unit> _toggle = new();
        private readonly Subject<Unit> _resume = new();
        private readonly Subject<Unit> _exit = new();

        private InputAction _cancel;

        public Observable<Unit> ToggleRequested => _toggle;
        public Observable<Unit> ResumeRequested => _resume;
        public Observable<Unit> ExitRequested => _exit;

        private void Awake()
        {
            _resumeButton.onClick.AddListener(() => _resume.OnNext(Unit.Default));
            _exitButton.onClick.AddListener(() => _exit.OnNext(Unit.Default));

            /// Действие берём из project-wide actions, а не заводим своё: в схеме UI уже
            /// есть Cancel — Esc на клавиатуре и B на геймпаде.
            _cancel = InputSystem.actions == null ? null : InputSystem.actions.FindAction("UI/Cancel");

            if (_cancel != null)
            {
                _cancel.performed += OnCancel;
                _cancel.Enable();
            }

            /// Панель — сам объект, как и у лобби: показывать и прятать её всё равно
            /// нужно целиком, а отдельная ссылка на себя только путала бы. Гасим здесь,
            /// а не галочкой в сцене: у выключенного объекта Unity не зовёт Awake, и
            /// подписки выше — включая Cancel — просто не появились бы. Разбудить объект
            /// до этого момента — забота BootstrapScope.
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_cancel != null) _cancel.performed -= OnCancel;

            _toggle.Dispose();
            _resume.Dispose();
            _exit.Dispose();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            SetCursorCaptured(false);
        }

        /// Гасит паузу и презентер из сцены стека, умирая вместе с ней, — а к тому
        /// моменту этот вид Unity уже могла снести: порядок разрушения между сценами
        /// не обещан никем.
        public void Hide(bool captureCursor)
        {
            if (this == null) return;

            gameObject.SetActive(false);
            if (captureCursor) SetCursorCaptured(true);
        }

        private void OnCancel(InputAction.CallbackContext context) => _toggle.OnNext(Unit.Default);

        /// Курсор — забота этого экрана: в матче его держит захваченным контроллер
        /// от Starter Assets, и без освобождения по кнопкам паузы не попасть. Обратно
        /// захват возвращаем только при возврате в матч — в лобби курсор нужен игроку.
        private static void SetCursorCaptured(bool captured)
        {
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }
    }
}
