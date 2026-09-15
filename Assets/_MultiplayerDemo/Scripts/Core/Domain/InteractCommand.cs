using Game.Net;

namespace Game.Core
{
    /// «Взаимодействовать» с сущностью под лучом (Ц15). Тела нет.
    public struct InteractCommand : INetMessage
    {
        public void Write(ref NetWriter writer)
        {
        }

        public void Read(ref NetReader reader)
        {
        }
    }
}
