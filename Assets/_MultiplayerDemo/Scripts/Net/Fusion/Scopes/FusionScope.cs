using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Net.Fusion
{
    /// Scope сцены Net_Fusion: всё, что знает про Fusion, живёт здесь и умирает вместе со
    /// сценой. Ни арены, ни презентеров — они соседи в BootstrapScope, а не дети стека.
    /// Отличие от NgoScope и MirrorScope ровно одно и оно от модели стека: менеджера в сцене
    /// нет, раннер одноразовый и его выдаёт фабрика.
    ///
    /// Родитель объявлен в инспекторе: parentReference = BootstrapScope. Сцену стека может
    /// открыть сам редактор — так делает виртуальный игрок MPPM, если она осталась у него
    /// в списке открытых, — и тогда scope поднимается до всякой загрузки потоком, остаётся
    /// без часов и гнезда и падает на первом же резолве.
    public sealed class FusionScope : LifetimeScope
    {
        [SerializeField] private FusionStackDefinition _stack;
        [SerializeField] private GameObject _runnerPrefab;
        [SerializeField] private EntityCatalog _entities;
        [SerializeField] private GameObject _sceneCarrier;

        protected override void Configure(IContainerBuilder builder)
        {
            /// Своё описание стека scope отдаёт базовым типом: лобби нужна подпись поля
            /// ручного ввода, а знать про Fusion оно не должно.
            builder.RegisterInstance<NetworkStackDefinition>(_stack);

            /// Lifetime.Singleton, а не Scoped: регистрация объявлена здесь, значит экземпляр
            /// создаётся здесь и умирает вместе со сценой стека. Scoped VContainer пересоздаёт
            /// в том контейнере, где резолвят, и scope арены получил бы свою копию сессии.
            builder.Register(container => new FusionRunnerFactory(container, _runnerPrefab), Lifetime.Singleton)
                   .AsImplementedInterfaces()
                   .AsSelf();

            /// Спавнер собирается руками: каталог и префаб носителя — поля этой сцены, а не
            /// зарегистрированные сервисы, и сквозь контейнер их тянуть незачем.
            builder.Register(container => new FusionSpawner(_entities, _sceneCarrier), Lifetime.Singleton)
                   .AsImplementedInterfaces()
                   .AsSelf();

            builder.Register<FusionDirectory>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<FusionSession>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<FusionStack>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<FusionRunnerCallbacks>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterEntryPoint<StackAttachment>();

            /// Мост получает раннер не сам, а через фабрику, и приходит к ней вызовом: мост
            /// просит сессию и каталог, а те просят фабрику — конструктором это кольцо не
            /// собрать.
            builder.RegisterBuildCallback(container =>
                container.Resolve<FusionRunnerFactory>().UseCallbacks(container.Resolve<FusionRunnerCallbacks>()));
        }
    }
}
