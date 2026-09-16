namespace Game.Net.Mirror
{
    /// То, что MirrorScope кладёт в гнездо. Собран из тех же трёх частей, что и у остальных
    /// стеков: для игры разницы между ними нет.
    public sealed class MirrorStack : INetworkStack
    {
        public NetworkStackDefinition Definition { get; }
        public INetSession Session { get; }
        public IHostDirectory Directory { get; }

        public MirrorStack(NetworkStackDefinition definition, MirrorSession session, MirrorDirectory directory)
        {
            Definition = definition;
            Session = session;
            Directory = directory;
        }
    }
}
