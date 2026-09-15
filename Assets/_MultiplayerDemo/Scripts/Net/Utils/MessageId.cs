namespace Game.Net
{
    /// Идентификатор типа сообщения — чистая функция от полного имени типа, одинаковая на
    /// всех машинах одной сборки. Статическое поле — мемоизация, а не реестр: таблицы нет,
    /// записать в неё нечего (исключение из правила «ни одного статического реестра»,
    /// записано в ограничениях фазы).
    public static class MessageId<T> where T : struct, INetMessage
    {
        public static readonly uint VALUE = StableHash.Of32(typeof(T).FullName);
    }
}
