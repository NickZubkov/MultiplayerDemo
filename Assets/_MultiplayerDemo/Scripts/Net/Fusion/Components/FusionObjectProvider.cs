using Fusion;
using VContainer;

namespace Game.Net.Fusion
{
    /// Третья точка подключения контейнера к спавну (Docs/Нулевой день.md § 5) и самая дешёвая
    /// из трёх: у Fusion провайдер объектов — обычный компонент на раннере, и если свой уже
    /// стоит, стек своего не добавляет. Всё, кроме создания экземпляра, оставляем базовому —
    /// сцены, повторные попытки и разбор prefab id у него виртуальные и рабочие.
    ///
    /// Создаёт экземпляр не сам: и свои объекты, и чужие рождаются одним путём — через спавнер,
    /// а тот зовёт фабрику мира. Иначе сетевой объект не увидел бы сервисов арены (А-4), а
    /// носитель остался бы без мира.
    public sealed class FusionObjectProvider : NetworkObjectProviderDefault
    {
        private FusionSpawner _spawner;

        [Inject]
        public void Construct(FusionSpawner spawner) => _spawner = spawner;

        protected override NetworkObject InstantiatePrefab(NetworkRunner runner, NetworkObject prefab) =>
            _spawner.Instantiate(prefab);
    }
}
