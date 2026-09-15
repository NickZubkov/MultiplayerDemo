using System;
using Game.Core;
using R3;
using VContainer.Unity;

namespace Game.UI
{
    /// Сообщения игроку из двух источников: отказы сценария (Failures) и отказы механик —
    /// логика ящика зовёт IHudMessages. Открыт всегда и ничего не забирает.
    public sealed class HudPresenter : IScreen, IHudMessages, IStartable, IDisposable
    {
        private readonly IHudView _view;
        private readonly IMatchFlow _match;
        private readonly ReactiveProperty<bool> _wanted = new(true);

        private DisposableBag _subscriptions;

        public ScreenLayer Layer => ScreenLayer.Overlay;
        public bool CapturesInput => false;
        public bool NeedsCursor => false;
        public ReadOnlyReactiveProperty<bool> Wanted => _wanted;

        public HudPresenter(IHudView view, IMatchFlow match)
        {
            _view = view;
            _match = match;
        }

        public void Start() => _match.Failures.Subscribe(Show).AddTo(ref _subscriptions);

        public void Dispose()
        {
            _subscriptions.Dispose();
            _wanted.Dispose();
        }

        public void Show() => _view.Show();

        public void Hide() => _view.Hide();

        public void Show(string message) => _view.ShowMessage(message);
    }
}
