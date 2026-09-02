using System.Collections.Generic;

namespace Game.Core
{
    /// Ключ — JoinToken: один хост остаётся одной строкой, сколько бы маяков ни прислал.
    public sealed class HostRegistry
    {
        public const double Ttl = 3.0;

        private readonly Dictionary<string, (HostEntry Entry, double SeenAt)> _seen = new();

        public void Report(HostEntry entry, double now) => _seen[entry.JoinToken] = (entry, now);

        public IReadOnlyList<HostEntry> GetAlive(double now)
        {
            var alive = new List<HostEntry>();
            var stale = new List<string>();

            foreach (var pair in _seen)
            {
                if (now - pair.Value.SeenAt < Ttl)
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
