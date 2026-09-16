using System;
using System.Collections.Generic;
using R3;

namespace Game.Net
{
    /// Поток списка хостов для тех стеков, что собирают его сами. Реестр каждый кадр отдаёт
    /// новый список, а ReactiveProperty сравнивает значения по ссылке — без сравнения по
    /// содержимому лобби перерисовывалось бы по шестьдесят раз в секунду.
    ///
    /// Пара «опубликовать — сравнить» жила копией в браузерах NGO и Mirror и совпадала
    /// построчно (И-18). Здесь она одна: правка сравнения теперь не может попасть в один
    /// стек и не попасть в другой.
    public sealed class HostListPublisher : IDisposable
    {
        private readonly ReactiveProperty<IReadOnlyList<HostEntry>> _hosts = new(Array.Empty<HostEntry>());

        public Observable<IReadOnlyList<HostEntry>> Hosts => _hosts;

        public void Dispose() => _hosts.Dispose();

        public void Publish(IReadOnlyList<HostEntry> hosts)
        {
            if (SameAsPublished(hosts)) return;

            _hosts.Value = hosts;
        }

        /// Сравниваем по содержимому, а не по длине: длина не меняется ни когда у хоста
        /// прибавился игрок, ни когда один хост сменился другим в том же кадре.
        private bool SameAsPublished(IReadOnlyList<HostEntry> hosts)
        {
            var published = _hosts.CurrentValue;
            if (published.Count != hosts.Count) return false;

            for (var i = 0; i < hosts.Count; i++)
            {
                if (!Same(published[i], hosts[i])) return false;
            }

            return true;
        }

        /// Метаданные тоже в сравнении: уровень хоста игрок видит в строке списка, и смена
        /// уровня между матчами обязана до неё дойти.
        private static bool Same(HostEntry published, HostEntry fresh)
        {
            if (published.JoinToken != fresh.JoinToken) return false;
            if (published.Players != fresh.Players) return false;
            if (published.MaxPlayers != fresh.MaxPlayers) return false;
            if (published.Name != fresh.Name) return false;

            return SameMetadata(published.Metadata, fresh.Metadata);
        }

        private static bool SameMetadata(IReadOnlyDictionary<string, string> published,
            IReadOnlyDictionary<string, string> fresh)
        {
            if (published.Count != fresh.Count) return false;

            foreach (var pair in fresh)
            {
                if (!published.TryGetValue(pair.Key, out var value) || value != pair.Value) return false;
            }

            return true;
        }
    }
}
