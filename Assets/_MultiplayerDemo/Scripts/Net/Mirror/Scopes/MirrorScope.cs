using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Net.Mirror
{
    /// Scope сцены Net_Mirror: всё, что знает про Mirror, живёт здесь и умирает вместе со
    /// сценой. Ни арены, ни презентеров — они соседи в BootstrapScope, а не дети стека.
    ///
    /// Родитель объявлен в инспекторе: parentReference = BootstrapScope. Сцену стека может
    /// открыть сам редактор — так делает виртуальный игрок MPPM, если она осталась у него
    /// в списке открытых, — и тогда scope поднимается до всякой загрузки потоком, остаётся
    /// без часов и гнезда и падает на первом же резолве.
    public sealed class MirrorScope : LifetimeScope
    {
        [SerializeField] private MirrorStackDefinition _stack;
        [SerializeField] private MirrorNetworkManager _manager;
        [SerializeField] private MirrorDiscovery _discovery;
        [SerializeField] private EntityCatalog _entities;
        [SerializeField] private GameObject _sceneCarrier;

        protected override void Configure(IContainerBuilder builder)
        {
            /// Своё описание стека scope отдаёт базовым типом: лобби нужна подпись поля
            /// ручного ввода, а знать про Mirror оно не должно.
            builder.RegisterInstance<NetworkStackDefinition>(_stack);

            /// Менеджер регистрируем базовым типом — сессии хватает NetworkManager,
            /// а мост дотягивается до наследника напрямую, минуя контейнер.
            builder.RegisterComponent<NetworkManager>(_manager);
            builder.RegisterComponent(_discovery);

            /// Lifetime.Singleton, а не Scoped: регистрация объявлена здесь, значит экземпляр
            /// создаётся здесь и умирает вместе со сценой стека. Scoped VContainer пересоздаёт
            /// в том контейнере, где резолвят, и scope арены получил бы свою копию сессии.
            builder.Register<MirrorPlayers>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();

            /// Спавнер собирается руками: каталог и префаб носителя — поля этой сцены, а не
            /// зарегистрированные сервисы, и сквозь контейнер их тянуть незачем.
            builder.Register(container => new MirrorSpawner(_entities, _sceneCarrier,
                    container.Resolve<MirrorPlayers>()), Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();

            builder.Register<MirrorDirectory>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<MirrorSession>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<MirrorStack>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterEntryPoint<StackAttachment>();

            /// Мост получает зависимости вызовом, а не инъекцией: MirrorSession просит менеджер
            /// конструктором, и [Inject] в обратную сторону замкнул бы кольцо, на котором
            /// сборка контейнера падает.
            builder.RegisterBuildCallback(container =>
                _manager.Bind(container.Resolve<MirrorSession>(),
                             container.Resolve<MirrorSpawner>(),
                             container.Resolve<MirrorPlayers>()));
        }
    }
}
