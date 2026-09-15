using System;
using System.Collections.Generic;
using R3;

namespace Game.Net.Local
{
    /// Каталог хостов стека «Без сети»: искать некого и подключаться не к кому.
    public sealed class EmptyDirectory : IHostDirectory
    {
        public Observable<IReadOnlyList<HostEntry>> Hosts =>
            Observable.Return<IReadOnlyList<HostEntry>>(Array.Empty<HostEntry>());

        public void Dispose()
        {
        }

        public void StartBrowsing()
        {
        }

        public void StopBrowsing()
        {
        }

        public bool TryParseManual(string text, out HostEntry entry)
        {
            entry = null;
            return false;
        }
    }
}
