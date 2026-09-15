using R3;

namespace Game.UI
{
    /// Экран паузы. Открывается клавишей, а не кнопкой в углу: в матче курсор захвачен,
    /// и попасть мышью по кнопке нельзя. Курсором вид не распоряжается — это UiService.
    public interface IPauseView
    {
        /// Игрок нажал «отмену» — показывать ли паузу, решает презентер: вне матча нечего.
        public Observable<Unit> ToggleRequested { get; }

        public Observable<Unit> ResumeRequested { get; }
        public Observable<Unit> ExitRequested { get; }

        public void Show();
        public void Hide();
    }
}
