namespace Game.Core
{
    /// Одна фаза приложения на всех: загружен ли стек и в каком состоянии сессия — сведено
    /// сюда сценарием матча, и презентеры смотрят только на неё (спека § 8.1).
    public enum AppPhase
    {
        SelectingStack,
        Lobby,
        Starting,
        InMatch,
        Leaving,
    }
}
