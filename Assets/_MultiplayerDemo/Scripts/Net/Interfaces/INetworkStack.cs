namespace Game.Net
{
    /// То, что scope стека кладёт в гнездо: кто мы, чем играть и где искать хозяев.
    public interface INetworkStack
    {
        public NetworkStackDefinition Definition { get; }
        public INetSession Session { get; }
        public IHostDirectory Directory { get; }
    }
}
