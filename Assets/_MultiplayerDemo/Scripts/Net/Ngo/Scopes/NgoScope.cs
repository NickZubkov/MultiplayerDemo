using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Net.Ngo
{
    /// Всё, что знает про NGO, живёт здесь и умирает вместе со сценой:
    /// сессия, браузер хостов, фабрика объектов, презентер лобби.
    public sealed class NgoScope : LifetimeScope
    {
        [SerializeField] private NetworkManager manager;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject cratePrefab;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(manager);
            builder.Register<NgoSessionControl>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.RegisterEntryPoint<NgoWorldSpawner>(
                       container => new NgoWorldSpawner(container, manager, cratePrefab), Lifetime.Scoped)
                   .AsSelf();

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
