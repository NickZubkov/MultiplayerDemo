namespace Game.Net.Fusion
{
    /// То, что FusionScope кладёт в гнездо. Собран из тех же трёх частей, что и у остальных
    /// стеков: для игры разницы между ними нет.
    public sealed class FusionStack : INetworkStack
    {
        public NetworkStackDefinition Definition { get; }
        public INetSession Session { get; }
        public IHostDirectory Directory { get; }

        public FusionStack(NetworkStackDefinition definition, FusionSession session, FusionDirectory directory)
        {
            Definition = definition;
            Session = session;
            Directory = directory;
        }
    }
}
