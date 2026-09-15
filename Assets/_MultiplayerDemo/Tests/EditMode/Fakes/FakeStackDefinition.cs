using Game.Net;

namespace Game.Tests
{
    /// Описание стека абстрактно, а тестам нужен экземпляр: поля выставляются
    /// через JsonUtility.FromJsonOverwrite — они приватные и сериализованные.
    public sealed class FakeStackDefinition : NetworkStackDefinition
    {
    }
}
