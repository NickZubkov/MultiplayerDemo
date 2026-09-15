using System;
using Cysharp.Threading.Tasks;
using Game.Core;
using R3;
using VContainer.Unity;

namespace Game.UI
{
    /// Пауза отделена от лобби (А-6): у LobbyPresenter было одиннадцать зависимостей и пять
    /// обязанностей. Вне матча пауза бессмысленна — выходить неоткуда.
    public sealed class PausePresenter : IScreen, IPausePresenter, IStartable, IDisposable
    {
        private readonly IPauseView _view;
        private readonly IMatchFlow _match;
        private readonly ReactiveProperty<bool> _wanted = new(false);

        private DisposableBag _subscriptions;

        public ScreenLayer Layer => ScreenLayer.Modal;
        public bool CapturesInput => true;
        public bool NeedsCursor => true;
        public ReadOnlyReactiveProperty<bool> Wanted => _wanted;
        public ReadOnlyReactiveProperty<bool> IsOpen => _wanted;

        public PausePresenter(IPauseView view, IMatchFlow match)
        {
            _view = view;
            _match = match;
        }

        public void Start()
        {
            _view.ToggleRequested.Subscribe(_ => Toggle()).AddTo(ref _subscriptions);
            _view.ResumeRequested.Subscribe(_ => Close()).AddTo(ref _subscriptions);
            _view.ExitRequested
                .SubscribeAwait((_, _) => ExitAsync(), AwaitOperation.Drop)
                .AddTo(ref _subscriptions);

            /// Матч кончился не по кнопке — отказ сессии: пауза уходит вместе с ним.
            _match.Phase
                .Where(phase => phase != AppPhase.InMatch)
                .Subscribe(_ => Close())
                .AddTo(ref _subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            _wanted.Dispose();
        }

        public void Open()
        {
            if (_match.Phase.CurrentValue == AppPhase.InMatch) _wanted.Value = true;
        }

        public void Close() => _wanted.Value = false;

        public void Show() => _view.Show();

        public void Hide() => _view.Hide();

        private void Toggle()
        {
            if (_wanted.Value)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        private async UniTask ExitAsync()
        {
            Close();
            await _match.LeaveAsync();
        }
    }
}
