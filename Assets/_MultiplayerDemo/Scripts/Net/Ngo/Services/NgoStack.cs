namespace Game.Net.Ngo
{
    /// То, что NgoScope кладёт в гнездо. Собран из тех же трёх частей, что и у остальных
    /// стеков: для игры разницы между ними нет.
    public sealed class NgoStack : INetworkStack
    {
        public NetworkStackDefinition Definition { get; }
        public INetSession Session { get; }
        public IHostDirectory Directory { get; }

        public NgoStack(NetworkStackDefinition definition, NgoSession session, NgoDirectory directory)
        {
            Definition = definition;
            Session = session;
            Directory = directory;
        }
    }
}
