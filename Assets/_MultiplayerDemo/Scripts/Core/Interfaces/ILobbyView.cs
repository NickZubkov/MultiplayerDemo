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

        public void Show();
        public void Hide();
        public void ShowHosts(IReadOnlyList<HostEntry> hosts);
        public void SetEmptyHint(string text);
    }
}
