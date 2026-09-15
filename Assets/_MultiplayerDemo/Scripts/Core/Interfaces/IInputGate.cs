using System;

namespace Game.Core
{
    /// Глушит игровой ввод, пока открыт экран, который его забирает (спека § 7.1).
    /// Живёт в Core, а не в Gameplay: UI берёт блокировку и при этом не должен ссылаться
    /// на игровой слой.
    public interface IInputGate
    {
        public bool IsBlocked { get; }

        public IDisposable Block();
    }
}
