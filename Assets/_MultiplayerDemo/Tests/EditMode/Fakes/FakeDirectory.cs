using System;
using System.Collections.Generic;
using Game.Net;
using R3;

namespace Game.Tests
{
    public sealed class FakeDirectory : IHostDirectory
    {
        public readonly ReactiveProperty<IReadOnlyList<HostEntry>> HostList =
            new(Array.Empty<HostEntry>());

        public bool Browsing;

        /// Что стек «разобрал» из ручного ввода: null — разобрать не удалось.
        public HostEntry Parsed;

        public Observable<IReadOnlyList<HostEntry>> Hosts => HostList;

        public void Dispose() => HostList.Dispose();

        public void StartBrowsing() => Browsing = true;

        public void StopBrowsing() => Browsing = false;

        public bool TryParseManual(string text, out HostEntry entry)
        {
            entry = Parsed;
            return Parsed != null;
        }
    }
}
