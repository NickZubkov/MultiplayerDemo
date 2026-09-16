namespace Game.Net.Ngo
{
    /// clientId у NGO — ulong, PlayerId — int. Сервер выдаёт номера подряд с нуля, и до
    /// предела int их не дорастить, но приведение всё равно проверяется: молча обрезанный
    /// номер был бы чужим игроком.
    public static class NgoIds
    {
        public static PlayerId Player(ulong clientId) => new(checked((int)clientId));

        public static ulong Client(PlayerId player) => (ulong)player.Value;
    }
}
