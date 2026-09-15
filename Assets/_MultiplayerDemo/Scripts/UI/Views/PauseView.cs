using R3;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI
{
    /// Курсором вид больше не распоряжается — это UiService. Esc — ссылка на действие,
    /// а не строка: обрыв ссылки виден в инспекторе, а не молчаливым null (И-23).
    public sealed class PauseView : MonoBehaviour, IPauseView
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _exitButton;
        [SerializeField] private InputActionReference _cancel;

        private readonly Subject<Unit> _toggle = new();
        private readonly Subject<Unit> _resume = new();
        private readonly Subject<Unit> _exit = new();

        public Observable<Unit> ToggleRequested => _toggle;
        public Observable<Unit> ResumeRequested => _resume;
        public Observable<Unit> ExitRequested => _exit;

        private void Awake()
        {
            _resumeButton.onClick.AddListener(() => _resume.OnNext(Unit.Default));
            _exitButton.onClick.AddListener(() => _exit.OnNext(Unit.Default));
            _cancel.action.performed += OnCancel;
            _cancel.action.Enable();
        }

        /// Включили — выключаем: действие project-wide, и включённым на весь запуск его
        /// оставлять нельзя (И-23).
        private void OnDestroy()
        {
            _cancel.action.performed -= OnCancel;
            _cancel.action.Disable();

            _toggle.Dispose();
            _resume.Dispose();
            _exit.Dispose();
        }

        public void Show() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);

        private void OnCancel(InputAction.CallbackContext _) => _toggle.OnNext(Unit.Default);
    }
}
