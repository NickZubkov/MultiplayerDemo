using System.Collections.Generic;

namespace Game.Net
{
    /// Ключ — JoinToken: один хост остаётся одной строкой, сколько бы маяков ни прислал.
    /// Живёт в Game.Net, а не в каталоге одного стека: реестр с TTL нужен каждому, кто ищет
    /// хостов сам, — сейчас это NGO, в задаче 15.10 к нему добавится Mirror.
    public sealed class HostRegistry
    {
        public const double TTL = 3.0;

        private readonly Dictionary<string, (HostEntry Entry, double SeenAt)> _seen = new();

        public void Report(HostEntry entry, double now) => _seen[entry.JoinToken] = (entry, now);

        public IReadOnlyList<HostEntry> GetAlive(double now)
        {
            var alive = new List<HostEntry>();
            var stale = new List<string>();

            foreach (var pair in _seen)
            {
                if (now - pair.Value.SeenAt < TTL)
                {
                    alive.Add(pair.Value.Entry);
                }
                else
                {
                    stale.Add(pair.Key);
                }
            }

            foreach (var token in stale)
            {
                _seen.Remove(token);
            }

            return alive;
        }
    }
}
