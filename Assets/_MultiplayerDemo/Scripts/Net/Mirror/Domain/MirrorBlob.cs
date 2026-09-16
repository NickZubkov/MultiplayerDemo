namespace Game.Net.Mirror
{
    /// Провод носителя Mirror: те же восемь слов и длина, что в NetBlob, но с изменяемыми
    /// публичными полями — читатель и писатель сгенерирует веавер (спайк, вопрос 3). У NetBlob
    /// поля readonly, и сгенерированный читатель не смог бы их присвоить.
    public struct MirrorBlob
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

        public static MirrorBlob From(in NetBlob blob) => new MirrorBlob
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
