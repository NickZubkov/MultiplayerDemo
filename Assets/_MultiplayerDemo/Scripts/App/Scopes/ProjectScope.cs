using Game.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.App
{
    /// Корень графа. Живёт всё время работы приложения.
    public sealed class ProjectScope : LifetimeScope
    {
        [SerializeField] private DemoConfig config;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(config);
            builder.Register<UnityClock>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
