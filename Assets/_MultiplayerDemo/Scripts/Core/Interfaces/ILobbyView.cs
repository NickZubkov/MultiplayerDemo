using System.Collections.Generic;
using R3;

namespace Game.Core
{
    public interface ILobbyView
    {
        public Observable<string> HostRequested { get; }
        public Observable<HostEntry> JoinRequested { get; }

        public void ShowHosts(IReadOnlyList<HostEntry> hosts);
        public void SetEmptyHint(string text);
    }
}
