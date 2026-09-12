using Game.App;
using Game.Core;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Net.Mirror
{
    /// Всё, что знает про Mirror, живёт здесь и умирает вместе со сценой: сессия,
    /// браузер хостов, фабрика объектов, спавнер, презентер лобби. Структурно —
    /// копия NgoScope, и это намеренно: одинаковый шаблон scope для трёх стеков
    /// и есть предмет сравнения.
    ///
    /// Родитель объявлен в инспекторе: parentReference = BootstrapScope. Сцену стека
    /// может открыть сам редактор — так делает виртуальный игрок MPPM, если она осталась
    /// у него в списке открытых, — и тогда scope поднимается до всякой загрузки потоком,
    /// остаётся без IClock и видов и падает на первом же резолве.
    public sealed class MirrorScope : LifetimeScope
    {
        [SerializeField] private MirrorStackDefinition _stack;
        [SerializeField] private MirrorNetworkManager _manager;
        [SerializeField] private MirrorDiscovery _discovery;
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private GameObject _cratePrefab;

        protected override void Configure(IContainerBuilder builder)
        {
            /// Своё описание стека scope отдаёт как базовый тип: презентеру лобби нужна
            /// подпись поля ручного ввода, а знать про Mirror он не должен.
            builder.RegisterInstance<NetworkStackDefinition>(_stack);

            /// Менеджер регистрируем базовым типом — сессии хватает NetworkManager,
            /// а мост дотягивается до наследника напрямую, минуя контейнер.
            builder.RegisterComponent<NetworkManager>(_manager);
            builder.RegisterComponent(_discovery);

            /// Lifetime.Singleton, а не Scoped, у всего в scope стека: Scoped VContainer
            /// пересоздаёт в том контейнере, где резолвят, и ArenaScope получил бы
            /// собственную копию IWorldSpawner — точки ушли бы в неё, а ящики не появились
            /// бы вовсе (решение S13 в спеке потока сцен).
            builder.Register<ArenaLoader>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<MirrorSessionControl>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<MirrorHostBrowser>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register(container => new MirrorObjectFactory(container, new[] { _playerPrefab, _cratePrefab }),
                       Lifetime.Singleton);
            builder.Register(container => new MirrorWorldSpawner(container.Resolve<MirrorObjectFactory>(),
                       _playerPrefab, _cratePrefab), Lifetime.Singleton)
                   .AsImplementedInterfaces()
                   .AsSelf();
            builder.RegisterEntryPoint<LobbyPresenter>(Lifetime.Singleton).AsSelf();

            /// Мост получает зависимости вызовом, а не инъекцией: MirrorSessionControl
            /// просит менеджер конструктором, и [Inject] в обратную сторону замкнул бы
            /// кольцо, на котором сборка контейнера падает.
            builder.RegisterBuildCallback(container =>
                _manager.Bind(container.Resolve<MirrorSessionControl>(),
                             container.Resolve<MirrorWorldSpawner>(),
                             container.Resolve<MirrorObjectFactory>()));
        }
    }
}
