using Game.Core;
using Game.UI;
using NUnit.Framework;
using R3;

namespace Game.Tests
{
    public sealed class PausePresenterTests
    {
        private sealed class FakePauseView : IPauseView
        {
            private readonly Subject<Unit> _toggle = new();
            private readonly Subject<Unit> _resume = new();
            private readonly Subject<Unit> _exit = new();

            public Observable<Unit> ToggleRequested => _toggle;
            public Observable<Unit> ResumeRequested => _resume;
            public Observable<Unit> ExitRequested => _exit;

            public void Show()
            {
            }

            public void Hide()
            {
            }

            public void PressCancel() => _toggle.OnNext(Unit.Default);

            public void ClickResume() => _resume.OnNext(Unit.Default);

            public void ClickExit() => _exit.OnNext(Unit.Default);
        }

        private static (PausePresenter presenter, FakePauseView view, FakeMatchFlow match) Rig()
        {
            var view = new FakePauseView();
            var match = new FakeMatchFlow();
            var presenter = new PausePresenter(view, match);
            presenter.Start();
            return (presenter, view, match);
        }

        /// В лобби пауза бессмысленна: выходить неоткуда.
        [Test]
        public void OpensOnlyInMatch()
        {
            var (presenter, view, match) = Rig();

            view.PressCancel();
            Assert.IsFalse(presenter.Wanted.CurrentValue);

            match.Current.Value = AppPhase.InMatch;
            view.PressCancel();
            Assert.IsTrue(presenter.Wanted.CurrentValue);
        }

        [Test]
        public void ResumeCloses()
        {
            var (presenter, view, match) = Rig();
            match.Current.Value = AppPhase.InMatch;
            view.PressCancel();

            view.ClickResume();

            Assert.IsFalse(presenter.Wanted.CurrentValue);
        }

        [Test]
        public void ExitClosesAndLeavesMatch()
        {
            var (presenter, view, match) = Rig();
            match.Current.Value = AppPhase.InMatch;
            view.PressCancel();

            view.ClickExit();

            Assert.IsFalse(presenter.Wanted.CurrentValue);
            Assert.IsTrue(match.Left);
        }

        /// Сессия отвалилась с открытой паузой — пауза уходит вместе с матчем.
        [Test]
        public void ClosesWhenMatchEnds()
        {
            var (presenter, view, match) = Rig();
            match.Current.Value = AppPhase.InMatch;
            view.PressCancel();

            match.Current.Value = AppPhase.Leaving;

            Assert.IsFalse(presenter.Wanted.CurrentValue);
        }
    }
}
