using System.Collections.Generic;
using Game.Core;
using Game.Net;
using R3;

namespace Game.UI
{
    /// Вид «глупый»: рисует готовые строки, сообщает о нажатиях и ничего не решает.
    /// Подпись строки хоста и подсказки собирает презентер.
    public interface ILobbyView
    {
        public Observable<string> HostRequested { get; }
        public Observable<HostEntry> JoinRequested { get; }

        /// Текст поля ручного ввода: разбирать его будет стек, не вид и не презентер.
        public Observable<string> ManualJoinRequested { get; }

        public Observable<ArenaDefinition> ArenaChosen { get; }
        public Observable<Unit> BackToStacksRequested { get; }

        public void Show();
        public void Hide();
        public void ShowHosts(IReadOnlyList<HostRow> hosts);
        public void ShowArenas(IReadOnlyList<ArenaDefinition> arenas);
        public void MarkArena(ArenaDefinition arena);
        public void SetEmptyHint(string text);
        public void SetManualHint(string text);
        public void SetHostLabel(string text);

        /// У «Без сети» подключаться не к кому — всё про подключение прячется.
        public void SetJoinVisible(bool visible);

        /// Пока идёт старт, кнопки выключены: второй заход ничего не даст.
        public void SetInteractable(bool interactable);
    }
}
