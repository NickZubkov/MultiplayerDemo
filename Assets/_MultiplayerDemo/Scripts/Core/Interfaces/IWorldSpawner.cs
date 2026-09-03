namespace Game.Core
{
    /// Наполнение мира на стороне хоста. Два метода, а не один: место для игрока
    /// стек спрашивает раньше, чем появляется сервер, — NGO делает это в подтверждении
    /// подключения внутри StartHost, — а предметы создаются уже после старта.
    public interface IWorldSpawner
    {
        public void UsePoints(ISpawnPointRegistry points);
        public void SpawnItems();
    }
}
