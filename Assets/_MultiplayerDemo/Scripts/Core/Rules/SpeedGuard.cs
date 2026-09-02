using System;

namespace Game.Core
{
    /// Граница доверия клиенту: за Δt нельзя пройти больше, чем maxSpeed × Δt × запас.
    /// Это не античит, а обозначенная граница — так и написано в README.
    public static class SpeedGuard
    {
        public const float DefaultTolerance = 1.5f;

        /// Допуск на дрожание сети и округление: без него пакеты с Δt около нуля
        /// ложно отклонялись бы на любом микросмещении.
        private const float Epsilon = 0.05f;

        public static bool IsPlausible(float distance, float deltaTime, float maxSpeed, float tolerance = DefaultTolerance)
        {
            if (distance < 0f) throw new ArgumentOutOfRangeException(nameof(distance));
            return distance <= maxSpeed * Math.Max(deltaTime, 0f) * tolerance + Epsilon;
        }
    }
}
