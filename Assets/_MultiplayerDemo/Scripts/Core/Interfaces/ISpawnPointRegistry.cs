using System.Collections.Generic;

namespace Game.Core
{
    public interface ISpawnPointRegistry
    {
        public IReadOnlyList<SpawnPoint> Players { get; }
        public IReadOnlyList<SpawnPoint> Items { get; }
    }
}
