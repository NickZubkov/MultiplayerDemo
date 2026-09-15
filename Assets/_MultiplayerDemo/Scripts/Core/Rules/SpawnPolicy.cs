using System.Collections.Generic;
using Game.Net;
using UnityEngine;

namespace Game.Core
{
    /// Куда ставить аватар — игровое правило, одно на все стеки (И-19). По номеру игрока,
    /// а не по счётчику: у Fusion аватар ставит себе каждый сам, и счётчик был бы у каждого
    /// свой. Двое в одной точке окажутся только при номерах, дающих один остаток, — для демки
    /// на четыре игрока и три-четыре точки это приемлемо.
    public static class SpawnPolicy
    {
        public static SpawnPoint For(PlayerId player, IReadOnlyList<SpawnPoint> points)
        {
            if (points == null || points.Count == 0 || player.IsNone)
            {
                return new SpawnPoint(Vector3.zero, Quaternion.identity);
            }

            return points[player.Value % points.Count];
        }
    }
}
