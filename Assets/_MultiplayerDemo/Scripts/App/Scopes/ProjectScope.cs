using Game.Core;
using R3;
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

            /// Кадры для публикаторов состояния сессии: отчёт стека доходит до подписчиков
            /// следующим кадром, уже за пределами его коллбэка (А-3).
            builder.RegisterInstance<FrameProvider>(UnityFrameProvider.Update);
        }
    }
}
