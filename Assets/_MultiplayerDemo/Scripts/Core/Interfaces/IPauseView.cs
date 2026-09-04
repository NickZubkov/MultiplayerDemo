using R3;

namespace Game.Core
{
    /// Экран паузы. Открывается клавишей, а не кнопкой в углу: в матче курсор захвачен
    /// контроллером от Starter Assets, и попасть мышью по кнопке нельзя.
    public interface IPauseView
    {
        /// Игрок нажал «отмену» — показывать ли паузу, решает презентер: вне матча нечего.
        public Observable<Unit> ToggleRequested { get; }

        public Observable<Unit> ResumeRequested { get; }
        public Observable<Unit> ExitRequested { get; }

        public void Show();

        /// captureCursor: true — возвращаемся в матч и курсор снова захватывается;
        /// false — уходим в лобби, где курсор нужен игроку.
        public void Hide(bool captureCursor);
    }
}
