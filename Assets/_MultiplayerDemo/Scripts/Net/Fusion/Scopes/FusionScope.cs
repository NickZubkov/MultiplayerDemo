using Game.App;
using Game.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Net.Fusion
{
    /// Всё, что знает про Fusion, живёт здесь и умирает вместе со сценой. Структурно —
    /// та же пятёрка, что в NgoScope и MirrorScope: сессия, браузер хостов, спавнер,
    /// ArenaLoader, LobbyPresenter. Отличие ровно одно и оно от модели стека: менеджера
    /// в сцене нет, раннер одноразовый и его выдаёт фабрика.
    ///
    /// Родитель объявлен в инспекторе: parentReference = BootstrapScope. Сцену стека может
    /// открыть сам редактор — так делает виртуальный игрок MPPM, если она осталась у него
    /// в списке открытых, — и тогда scope поднимается до всякой загрузки потоком, остаётся
    /// без IClock и видов и падает на первом же резолве.
    public sealed class FusionScope : LifetimeScope
    {
        [SerializeField] private FusionStackDefinition _stack;
        [SerializeField] private GameObject _runnerPrefab;
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private GameObject _cratePrefab;

        protected override void Configure(IContainerBuilder builder)
        {
            /// Своё описание стека scope отдаёт как базовый тип: презентеру лобби нужна
            /// подпись поля ручного ввода, а знать про Fusion он не должен.
            builder.RegisterInstance<NetworkStackDefinition>(_stack);

            /// Lifetime.Singleton, а не Scoped, у всего в scope стека: Scoped VContainer
            /// пересоздаёт в том контейнере, где резолвят, и ArenaScope получил бы
            /// собственную копию IWorldSpawner — точки ушли бы в неё, а ящики не появились
            /// бы вовсе (решение S13 в спеке потока сцен).
            builder.Register<ArenaLoader>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register(container => new FusionRunnerFactory(container, _runnerPrefab), Lifetime.Singleton)
                   .AsImplementedInterfaces()
                   .AsSelf();
            builder.Register<FusionSessionControl>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<FusionHostBrowser>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register(container => new FusionWorldSpawner(container.Resolve<FusionRunnerFactory>(),
                       _playerPrefab, _cratePrefab), Lifetime.Singleton)
                   .AsImplementedInterfaces()
                   .AsSelf();
            builder.Register<FusionRunnerCallbacks>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterEntryPoint<LobbyPresenter>(Lifetime.Singleton).AsSelf();

            /// Мост получает раннер не сам, а через фабрику, и приходит к ней вызовом:
            /// мост просит сессию и браузер, а те просят фабрику — конструктором это
            /// кольцо не собрать.
            builder.RegisterBuildCallback(container =>
                container.Resolve<FusionRunnerFactory>().UseCallbacks(container.Resolve<FusionRunnerCallbacks>()));
        }
    }
}
