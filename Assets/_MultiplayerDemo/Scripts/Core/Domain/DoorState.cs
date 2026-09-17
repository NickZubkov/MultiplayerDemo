using Game.Net;

namespace Game.Core
{
    /// Всё состояние двери — один флаг. Угол створки и ход анимации выводит из него оболочка
    /// на каждой машине: по сети едет решение, а не картинка.
    public struct DoorState : INetMessage
    {
        private bool _open;

        public bool Open => _open;

        public DoorState(bool open)
        {
            _open = open;
        }

        public void Write(ref NetWriter writer) => writer.WriteBool(_open);

        public void Read(ref NetReader reader) => _open = reader.ReadBool();
    }
}
