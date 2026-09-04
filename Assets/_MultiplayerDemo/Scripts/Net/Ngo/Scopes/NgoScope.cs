using Game.App;
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
        [SerializeField] private NetworkManager manager;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject cratePrefab;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(manager);

            /// ArenaLoader просит LifetimeScope, чтобы накрыть сцену арены родителем.
            /// Регистрировать scope не нужно: VContainer делает это сам последней строкой
            /// LifetimeScope.InstallTo, и вторая такая регистрация ломает сборку контейнера
            /// конфликтом типов реализации.
            builder.Register<ArenaLoader>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.Register<NgoSessionControl>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.Register<NgoHostBrowser>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.RegisterEntryPoint<NgoWorldSpawner>(
                       container => new NgoWorldSpawner(container, manager, cratePrefab), Lifetime.Scoped)
                   .AsSelf();
            builder.RegisterEntryPoint<LobbyPresenter>(Lifetime.Scoped).AsSelf();

            /// Оба префаба создаёт сеть, а не контейнер: игрока NGO спавнит сама при
            /// подключении, ящики — хост. Обработчик ставим на уже собранный контейнер
            /// и до старта сессии; переживать перезапуск хоста ему не нужно — менеджер
            /// живёт в этой же сцене и умирает вместе со scope.
            builder.RegisterBuildCallback(container =>
            {
                manager.PrefabHandler.AddHandler(playerPrefab, new NgoObjectFactory(container, playerPrefab));
                manager.PrefabHandler.AddHandler(cratePrefab, new NgoObjectFactory(container, cratePrefab));
            });
        }
    }
}
