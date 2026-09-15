using System;
using System.Collections.Generic;
using Game.Net;
using R3;

namespace Game.Core
{
    /// Откуда берётся список хостов: UDP-широковещание, штатный поиск стека или облако.
    /// Поток, а не событие: записи приходят во времени и истекают по TTL —
    /// это ровно та задача, для которой R3 и нужен.
    public interface IHostBrowser : IDisposable
    {
        public Observable<IReadOnlyList<HostEntry>> Hosts { get; }

        public void StartBrowsing();
        public void StopBrowsing();
    }
}
