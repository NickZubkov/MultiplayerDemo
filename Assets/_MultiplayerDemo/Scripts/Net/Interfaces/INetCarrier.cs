namespace Game.Net
{
    /// Сторона стека. Носитель переносит байты и не знает, что внутри; отправителя команды
    /// он узнаёт сам — из RPC своего стека — и отдаёт каналу вместе с телом.
    public interface INetCarrier
    {
        public NetEntityId Id { get; }
        public PlayerId Owner { get; }
        public bool IsAuthority { get; }

        public void PublishState(in NetBlob state);
        public void SendToAuthority(uint type, in NetBlob payload);
        public void SendToPlayer(PlayerId target, uint type, in NetBlob payload);
        public void SendToAll(uint type, in NetBlob payload);
        public void Snap();
    }
}
