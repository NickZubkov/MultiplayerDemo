namespace Game.Net
{
    /// FNV-1a: одинаковый результат на любой машине и в любом запуске, в отличие от
    /// string.GetHashCode, который в .NET вправе меняться между процессами.
    public static class StableHash
    {
        private const ulong OFFSET_64 = 14695981039346656037;
        private const ulong PRIME_64 = 1099511628211;
        private const uint OFFSET_32 = 2166136261;
        private const uint PRIME_32 = 16777619;

        public static ulong Of64(string text)
        {
            var hash = OFFSET_64;

            foreach (var symbol in text)
            {
                hash = (hash ^ symbol) * PRIME_64;
            }

            return hash;
        }

        public static uint Of32(string text)
        {
            var hash = OFFSET_32;

            foreach (var symbol in text)
            {
                hash = (hash ^ symbol) * PRIME_32;
            }

            return hash;
        }
    }
}
