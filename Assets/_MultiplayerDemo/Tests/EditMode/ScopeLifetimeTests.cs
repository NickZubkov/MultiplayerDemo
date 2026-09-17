using NUnit.Framework;
using VContainer;

namespace Game.Tests
{
    /// Регрессия на ловушку, из-за которой в аренах когда-то перестали появляться ящики:
    /// сервис был зарегистрирован Lifetime.Scoped, дочерний контейнер получил от VContainer
    /// свежую копию, и половина кода работала с одним экземпляром, половина — с другим.
    /// Поэтому в scope стека и арены всё регистрируется Singleton: экземпляр всё равно
    /// живёт и умирает вместе со своим scope, а дочерние видят ровно его.
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
