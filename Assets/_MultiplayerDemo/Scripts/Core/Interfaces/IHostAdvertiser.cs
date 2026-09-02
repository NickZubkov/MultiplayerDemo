namespace Game.Core
{
    /// Кто умеет рассказывать о себе. У облачных реализаций этим занимается сервис,
    /// поэтому интерфейс отдельный от IHostBrowser.
    public interface IHostAdvertiser
    {
        public void Advertise(string hostName, int players, int maxPlayers);
    }
}
