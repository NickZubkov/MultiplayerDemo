using Game.Net;

namespace Game.Core
{
    /// Всё состояние ящика — одно поле. Руки, кинематика и поза в руке выводятся из него
    /// на каждой машине, а не хранятся рядом копиями (А-5).
    public struct CrateState : INetMessage
    {
        private PlayerId _holder;

        public PlayerId Holder => _holder;

        public CrateState(PlayerId holder)
        {
            _holder = holder;
        }

        public void Write(ref NetWriter writer) => writer.WritePlayerId(_holder);

        public void Read(ref NetReader reader) => _holder = reader.ReadPlayerId();
    }
}
