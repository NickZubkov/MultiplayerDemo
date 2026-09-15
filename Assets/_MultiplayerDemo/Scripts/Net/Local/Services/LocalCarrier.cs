namespace Game.Net.Local
{
    public sealed class LocalCarrier : INetCarrier
    {
        private readonly LocalNetwork _network;

        public NetEntityId Id { get; }
        public PlayerId Owner { get; }
        public LocalMachine Machine { get; }
        public NetEntityChannel Channel { get; private set; }
        public bool IsAuthority => _network.IsAuthority(Machine, Owner);

        internal LocalCarrier(LocalNetwork network, LocalMachine machine, NetEntityId id, PlayerId owner)
        {
            _network = network;
            Machine = machine;
            Id = id;
            Owner = owner;
        }

        internal void Attach(NetEntityChannel channel) => Channel = channel;

        public void PublishState(in NetBlob state) => _network.Publish(this, state);

        public void SendToAuthority(uint type, in NetBlob payload) => _network.ToAuthority(this, type, payload);

        public void SendToPlayer(PlayerId target, uint type, in NetBlob payload) =>
            _network.ToPlayer(this, target, type, payload);

        public void SendToAll(uint type, in NetBlob payload) => _network.ToAll(this, type, payload);

        /// В памяти движение не рвётся: интерполяции, которую надо сбрасывать, здесь нет.
        public void Snap()
        {
        }
    }
}
