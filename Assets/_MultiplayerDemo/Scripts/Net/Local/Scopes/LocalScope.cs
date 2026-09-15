using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Net.Local
{
    /// Scope сцены Net_Local. Родитель объявлен в инспекторе (parentReference =
    /// BootstrapScope), как у всех сцен стеков: сцену может открыть сам редактор — так
    /// делает виртуальный игрок MPPM, — и без родителя scope упадёт на первом резолве.
    public sealed class LocalScope : LifetimeScope
    {
        [SerializeField] private LocalStackDefinition _stack;
        [SerializeField] private EntityCatalog _entities;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance<NetworkStackDefinition>(_stack);
            builder.RegisterInstance(_entities);
            builder.Register<LocalSession>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EmptyDirectory>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<LocalStack>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterEntryPoint<StackAttachment>();
        }
    }
}
