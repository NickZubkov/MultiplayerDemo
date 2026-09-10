using Fusion;
using VContainer;
using VContainer.Unity;

namespace Game.Net.Fusion
{
    /// Третья точка подключения контейнера к спавну (Docs/Нулевой день.md § 5) и самая
    /// дешёвая из трёх: у Fusion провайдер объектов — обычный компонент на раннере, и если
    /// свой уже стоит, стек своего не добавляет. Всё, кроме создания экземпляра, оставляем
    /// базовому — сцены, повторные попытки и разбор prefab id у него виртуальные и рабочие.
    public sealed class FusionObjectProvider : NetworkObjectProviderDefault
    {
        private IObjectResolver _resolver;

        [Inject]
        public void Construct(IObjectResolver resolver) => _resolver = resolver;

        protected override NetworkObject InstantiatePrefab(NetworkRunner runner, NetworkObject prefab) =>
            _resolver.Instantiate(prefab.gameObject).GetComponent<NetworkObject>();
    }
}
