using Game.Gameplay;
using Game.UI;
using NUnit.Framework;
using R3;

namespace Game.Tests
{
    /// Общее поведение экранов (спека § 8.2): сервис не знает ни одного экрана по имени.
    public sealed class UiServiceTests
    {
        private sealed class FakeScreen : IScreen
        {
            public readonly ReactiveProperty<bool> Want = new(false);

            public bool Visible = true;

            public ScreenLayer Layer { get; }
            public bool CapturesInput { get; }
            public bool NeedsCursor { get; }
            public ReadOnlyReactiveProperty<bool> Wanted => Want;

            public FakeScreen(ScreenLayer layer, bool capturesInput = false, bool needsCursor = false)
            {
                Layer = layer;
                CapturesInput = capturesInput;
                NeedsCursor = needsCursor;
            }

            public void Show() => Visible = true;

            public void Hide() => Visible = false;
        }

        private sealed class FakeCursor : ICursor
        {
            public bool Free;

            public void SetFree(bool free) => Free = free;
        }

        /// Экран в сцене мог остаться включённым после вёрстки — начальное состояние задаёт сервис.
        [Test]
        public void EveryScreenIsHiddenOnStart()
        {
            var screen = new FakeScreen(ScreenLayer.Base);
            var ui = new UiService(new IScreen[] { screen }, new InputGate(), new FakeCursor());

            ui.Start();

            Assert.IsFalse(screen.Visible);
        }

        [Test]
        public void BaseScreenReplacesAnotherBase()
        {
            var lobby = new FakeScreen(ScreenLayer.Base);
            var stacks = new FakeScreen(ScreenLayer.Base);
            var ui = new UiService(new IScreen[] { lobby, stacks }, new InputGate(), new FakeCursor());
            ui.Start();

            lobby.Want.Value = true;
            stacks.Want.Value = true;

            Assert.IsFalse(lobby.Visible);
            Assert.IsTrue(stacks.Visible);
        }

        [Test]
        public void OverlayAndModalKeepBaseOpen()
        {
            var lobby = new FakeScreen(ScreenLayer.Base);
            var hud = new FakeScreen(ScreenLayer.Overlay);
            var pause = new FakeScreen(ScreenLayer.Modal);
            var ui = new UiService(new IScreen[] { lobby, hud, pause }, new InputGate(), new FakeCursor());
            ui.Start();

            lobby.Want.Value = true;
            hud.Want.Value = true;
            pause.Want.Value = true;

            Assert.IsTrue(lobby.Visible && hud.Visible && pause.Visible);
        }

        [Test]
        public void CapturingScreenBlocksInputUntilClosed()
        {
            var gate = new InputGate();
            var pause = new FakeScreen(ScreenLayer.Modal, capturesInput: true);
            var ui = new UiService(new IScreen[] { pause }, gate, new FakeCursor());
            ui.Start();

            pause.Want.Value = true;
            Assert.IsTrue(gate.IsBlocked);

            pause.Want.Value = false;
            Assert.IsFalse(gate.IsBlocked);
        }

        /// Курсором владеет один сервис: свободен, пока открыт экран, которому он нужен.
        [Test]
        public void CursorIsFreeOnlyWhileNeeded()
        {
            var cursor = new FakeCursor();
            var hud = new FakeScreen(ScreenLayer.Overlay);
            var pause = new FakeScreen(ScreenLayer.Modal, needsCursor: true);
            var ui = new UiService(new IScreen[] { hud, pause }, new InputGate(), cursor);
            ui.Start();

            hud.Want.Value = true;
            Assert.IsFalse(cursor.Free);

            pause.Want.Value = true;
            Assert.IsTrue(cursor.Free);

            pause.Want.Value = false;
            Assert.IsFalse(cursor.Free);
        }
    }
}
