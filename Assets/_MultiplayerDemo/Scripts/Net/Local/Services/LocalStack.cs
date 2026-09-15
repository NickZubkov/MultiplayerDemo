namespace Game.Net.Local
{
    /// То, что LocalScope кладёт в гнездо. Собран из тех же трёх частей, что и у сетевых
    /// стеков: разницы между «Без сети» и остальными для игры нет.
    public sealed class LocalStack : INetworkStack
    {
        public NetworkStackDefinition Definition { get; }
        public INetSession Session { get; }
        public IHostDirectory Directory { get; }

        public LocalStack(NetworkStackDefinition definition, LocalSession session, EmptyDirectory directory)
        {
            Definition = definition;
            Session = session;
            Directory = directory;
        }
    }
}
