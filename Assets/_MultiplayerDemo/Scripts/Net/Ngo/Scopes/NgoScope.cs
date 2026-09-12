using Game.App;
using Game.Core;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Net.Ngo
{
    /// Всё, что знает про NGO, живёт здесь и умирает вместе со сценой:
    /// сессия, браузер хостов, фабрика объектов, презентер лобби.
    ///
    /// Родитель объявлен в инспекторе: parentReference = BootstrapScope. Сцену стека может
    /// открыть сам редактор — так делает виртуальный игрок MPPM, если она осталась у него
    /// в списке открытых, — и тогда scope поднимается до всякой загрузки потоком, остаётся
    /// без IClock и видов и падает на первом же резолве.
    ///
    /// StackSelectPresenter при загрузке сцены всё равно накрывает её EnqueueParent, но до
    /// него дело уже не доходит: в LifetimeScope.GetRuntimeParent() поле инспектора стоит
    /// раньше очереди EnqueueParent. Он остаётся страховкой для сцен Mirror и Fusion —
    /// на случай, если там забудут проставить parentReference.
    public sealed class NgoScope : LifetimeScope
    {
        [SerializeField] private NgoStackDefinition _stack;
        [SerializeField] private NetworkManager _manager;
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private GameObject _cratePrefab;

        protected override void Configure(IContainerBuilder builder)
        {
            /// Своё описание стека scope отдаёт как базовый тип: презентеру лобби нужна
            /// подпись поля ручного ввода, а знать про NGO он не должен.
            builder.RegisterInstance<NetworkStackDefinition>(_stack);
            builder.RegisterComponent(_manager);

            /// Lifetime.Singleton, а не Scoped, у всего в этом scope — и это не про «один
            /// на приложение»: регистрация объявлена здесь, поэтому экземпляр создаётся здесь
            /// и умирает вместе со сценой стека. Разница в дочерних scope: Scoped VContainer
            /// пересоздаёт в том контейнере, где резолвят (Container.cs:161), а Singleton
            /// поднимает к родителю. Из-за Scoped ArenaScope получал собственную копию
            /// IWorldSpawner, отдавал точки ей, и ящики не появлялись — спавнер, которого
            /// звал презентер, оставался без точек.
            ///
            /// ArenaLoader просит LifetimeScope, чтобы накрыть сцену арены родителем.
            /// Регистрировать scope не нужно: VContainer делает это сам последней строкой
            /// LifetimeScope.InstallTo, и вторая такая регистрация ломает сборку контейнера
            /// конфликтом типов реализации.
            builder.Register<ArenaLoader>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<NgoSessionControl>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<NgoHostBrowser>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterEntryPoint<NgoWorldSpawner>(
                       container => new NgoWorldSpawner(container, _manager, _cratePrefab), Lifetime.Singleton)
                   .AsSelf();
            builder.RegisterEntryPoint<LobbyPresenter>(Lifetime.Singleton).AsSelf();

            /// Оба префаба создаёт сеть, а не контейнер: игрока NGO спавнит сама при
            /// подключении, ящики — хост. Обработчик ставим на уже собранный контейнер
            /// и до старта сессии; переживать перезапуск хоста ему не нужно — менеджер
            /// живёт в этой же сцене и умирает вместе со scope.
            builder.RegisterBuildCallback(container =>
            {
                _manager.PrefabHandler.AddHandler(_playerPrefab, new NgoObjectFactory(container, _playerPrefab));
                _manager.PrefabHandler.AddHandler(_cratePrefab, new NgoObjectFactory(container, _cratePrefab));
            });
        }
    }
}
