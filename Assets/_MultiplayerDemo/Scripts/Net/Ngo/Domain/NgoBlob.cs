using Unity.Netcode;

namespace Game.Net.Ngo
{
    /// Провод носителя NGO: те же восемь слов и длина, что в NetBlob, но с изменяемыми полями и
    /// копированием памятью — NetworkVariable и RPC переносят такую структуру без своего кода
    /// (спайк, вопрос 3). NetBlob про NGO не знает и знать не должен.
    public struct NgoBlob : INetworkSerializeByMemcpy
    {
        public ulong W0;
        public ulong W1;
        public ulong W2;
        public ulong W3;
        public ulong W4;
        public ulong W5;
        public ulong W6;
        public ulong W7;
        public byte Length;

        public static NgoBlob From(in NetBlob blob) => new NgoBlob
        {
            W0 = blob.W0,
            W1 = blob.W1,
            W2 = blob.W2,
            W3 = blob.W3,
            W4 = blob.W4,
            W5 = blob.W5,
            W6 = blob.W6,
            W7 = blob.W7,
            Length = blob.Length,
        };

        public NetBlob ToNet() => new(W0, W1, W2, W3, W4, W5, W6, W7, Length);
    }
}
