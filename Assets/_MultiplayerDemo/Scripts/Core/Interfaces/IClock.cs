namespace Game.Core
{
    /// Время за интерфейсом — чтобы презентеры и каталоги хостов тестировались без плеера.
    /// Заведён потому, что подменяемых часов из коробки нет: тип TimeProvider доступен,
    /// но FakeTimeProvider не собран под netstandard2.1 ни в одной версии пакета
    /// (решение нулевого дня, Docs/Нулевой день.md § 4).
    public interface IClock
    {
        public double Now { get; }
    }
}
