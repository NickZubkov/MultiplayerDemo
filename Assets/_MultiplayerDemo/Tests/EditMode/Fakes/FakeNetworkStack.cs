using Game.Net;

namespace Game.Tests
{
    public sealed class FakeNetworkStack : INetworkStack
    {
        public NetworkStackDefinition Definition { get; }
        public INetSession Session { get; }
        public IHostDirectory Directory { get; }

        public FakeNetworkStack(NetworkStackDefinition definition, INetSession session, IHostDirectory directory)
        {
            Definition = definition;
            Session = session;
            Directory = directory;
        }
    }
}
