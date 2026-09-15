using Game.Core;

namespace Game.Tests
{
    public sealed class FakeHud : IHudMessages
    {
        public string Last;

        public void Show(string message) => Last = message;
    }
}
