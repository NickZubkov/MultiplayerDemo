using System.Collections.Generic;
using R3;

namespace Game.Core
{
    /// Show и Hide здесь потому, что экран лобби переживает всю сессию: в арене он
    /// перекрывал бы обзор, а после отказа обязан вернуться вместе с причиной.
    public interface ILobbyView
    {
        public Observable<string> HostRequested { get; }
        public Observable<HostEntry> JoinRequested { get; }
        public Observable<ArenaDefinition> ArenaChosen { get; }
        public Observable<Unit> BackToStacksRequested { get; }

        public void Show();
        public void Hide();
        public void ShowHosts(IReadOnlyList<HostEntry> hosts);

        /// Каталог отдаётся целиком: по нему вид не только рисует колонку уровней,
        /// но и подписывает строки хостов — в маяке едет id, а игрок читает название.
        public void ShowArenas(IReadOnlyList<ArenaDefinition> arenas);
        public void MarkArena(ArenaDefinition arena);
        public void SetEmptyHint(string text);
        public void SetManualHint(string text);
    }
}
