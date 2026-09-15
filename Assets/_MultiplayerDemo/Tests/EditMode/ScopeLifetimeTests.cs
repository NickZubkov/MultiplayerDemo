using NUnit.Framework;
using VContainer;

namespace Game.Tests
{
    /// Регрессия на ловушку, из-за которой в аренах перестали появляться ящики.
    /// ArenaScope резолвит IWorldSpawner в собственном контейнере, и при Lifetime.Scoped
    /// VContainer отдаёт ему свежую копию — точки уходили не тому спавнеру, а тот, кого
    /// звал презентер, оставался пустым. Поэтому в scope стека всё регистрируется
    /// Singleton: экземпляр всё равно живёт и умирает вместе с этим scope.
    public sealed class ScopeLifetimeTests
    {
        private sealed class Service
        {
        }

        [Test]
        public void ScopedServiceIsRecreatedInChildScope()
        {
            var builder = new ContainerBuilder();
            builder.Register<Service>(Lifetime.Scoped);

            using var parent = builder.Build();
            using var child = parent.CreateScope();

            Assert.AreNotSame(parent.Resolve<Service>(), child.Resolve<Service>());
        }

        [Test]
        public void SingletonServiceIsSharedWithChildScope()
        {
            var builder = new ContainerBuilder();
            builder.Register<Service>(Lifetime.Singleton);

            using var parent = builder.Build();
            using var child = parent.CreateScope();

            Assert.AreSame(parent.Resolve<Service>(), child.Resolve<Service>());
        }
    }
}
