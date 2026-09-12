using Game.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.App
{
    /// Корень графа. Живёт всё время работы приложения.
    public sealed class ProjectScope : LifetimeScope
    {
        [SerializeField] private DemoConfig _config;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_config);
            builder.Register<UnityClock>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
