using Mirror;

namespace Game.Net.Mirror
{
    /// Первое, что сервер говорит подключившемуся, — его номер в сессии. Приходит раньше любого
    /// спавна: сообщения идут одним надёжным каналом по порядку.
    public struct MirrorWelcome : NetworkMessage
    {
        public int Player;
    }
}
