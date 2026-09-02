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

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(manager);
            builder.Register<NgoSessionControl>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
        }
    }
}
