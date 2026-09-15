using R3;

namespace Game.UI
{
    /// Экран описывает себя сам (спека § 8.2) и сам решает, когда хочет быть открытым.
    /// Что показать на самом деле, решает UiService: Wanted — желание, Show/Hide — решение.
    public interface IScreen
    {
        public ScreenLayer Layer { get; }
        public bool CapturesInput { get; }
        public bool NeedsCursor { get; }
        public ReadOnlyReactiveProperty<bool> Wanted { get; }

        public void Show();
        public void Hide();
    }
}
