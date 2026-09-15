namespace Game.Net.Local
{
    public sealed class LocalMachine
    {
        private readonly LocalNetwork _network;

        public PlayerId Player { get; }
        public bool IsJudge => _network.Judge == this;

        internal LocalMachine(LocalNetwork network, PlayerId player)
        {
            _network = network;
            Player = player;
        }

        /// Сущность мира — владелец None, авторитет у судьи; аватар — владелец его игрок.
        public NetEntityChannel CreateEntity(NetEntityId id, PlayerId owner) => _network.Create(this, id, owner);
    }
}
