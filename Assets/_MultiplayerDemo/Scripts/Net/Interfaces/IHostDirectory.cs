using System;
using System.Collections.Generic;
using R3;

namespace Game.Net
{
    /// Сбой сети не гасит поиск навсегда: реализация переживает ошибку и продолжает (И-5).
    /// Разбор ручного ввода — дело стека: у LAN это адрес, у Fusion — имя сессии.
    public interface IHostDirectory : IDisposable
    {
        public Observable<IReadOnlyList<HostEntry>> Hosts { get; }

        public void StartBrowsing();
        public void StopBrowsing();
        public bool TryParseManual(string text, out HostEntry entry);
    }
}
