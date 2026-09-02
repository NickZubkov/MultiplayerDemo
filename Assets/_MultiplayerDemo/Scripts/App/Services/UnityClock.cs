using Game.Core;
using UnityEngine;

namespace Game.App
{
    /// Реализация часов поверх времени плеера. Монотонная и не зависит от системной
    /// даты — для TTL в HostRegistry важно именно это, а не календарное время.
    public sealed class UnityClock : IClock
    {
        public double Now => Time.realtimeSinceStartupAsDouble;
    }
}
