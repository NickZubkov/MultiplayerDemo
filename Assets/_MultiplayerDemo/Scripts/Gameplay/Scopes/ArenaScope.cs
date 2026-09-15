using Game.Core;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay
{
    /// Scope сцены арены. Арена по-прежнему не знает, какой стек её грузит: она собирает
    /// свой INetWorld, а кто им воспользуется — дело сессии.
    ///
    /// Родитель — BootstrapScope, проставлен в инспекторе: у арены он теперь один на все стеки.
    public sealed class ArenaScope : LifetimeScope
    {
        /// Всё зарегистрированное здесь видят и сетевые сущности: их создаёт фабрика арены,
        /// то есть этот контейнер (А-4 закрыт построением).
        protected override void Configure(IContainerBuilder builder)
        {
            /// Ищет только в своей сцене, включая выключенные объекты, и падает с внятным
            /// сообщением, если точек нет: арена без маркеров — сломанная сцена, а не норма.
            builder.RegisterComponentInHierarchy<ArenaSpawnPoints>().AsSelf();
            builder.Register<EntityFactory>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ArenaWorld>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<HoldRegistry>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<AvatarRegistry>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterEntryPoint<SpeedGuardService>();
        }
    }
}
