using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Net.Ngo
{
    /// Scope сцены Net_Ngo: всё, что знает про NGO, живёт здесь и умирает вместе со сценой.
    /// Ни арены, ни презентеров — они соседи в BootstrapScope, а не дети стека.
    ///
    /// Родитель объявлен в инспекторе: parentReference = BootstrapScope. Сцену стека может
    /// открыть сам редактор — так делает виртуальный игрок MPPM, если она осталась у него
    /// в списке открытых, — и тогда scope поднимается до всякой загрузки потоком, остаётся
    /// без часов и гнезда и падает на первом же резолве.
    public sealed class NgoScope : LifetimeScope
    {
        [SerializeField] private NgoStackDefinition _stack;
        [SerializeField] private NetworkManager _manager;
        [SerializeField] private EntityCatalog _entities;
        [SerializeField] private GameObject _sceneCarrier;

        protected override void Configure(IContainerBuilder builder)
        {
            /// Своё описание стека scope отдаёт базовым типом: лобби нужна подпись поля
            /// ручного ввода, а знать про NGO оно не должно.
            builder.RegisterInstance<NetworkStackDefinition>(_stack);
            builder.RegisterComponent(_manager);

            /// Спавнер собирается руками: каталог и префаб носителя — поля этой сцены, а не
            /// зарегистрированные сервисы, и сквозь контейнер их тянуть незачем.
            builder.RegisterEntryPoint<NgoSpawner>(
                       container => new NgoSpawner(_manager, _entities, _sceneCarrier), Lifetime.Singleton)
                   .AsSelf();

            /// Lifetime.Singleton, а не Scoped: регистрация объявлена здесь, значит экземпляр
            /// создаётся здесь и умирает вместе со сценой стека. Scoped VContainer пересоздаёт
            /// в том контейнере, где резолвят, и scope арены получил бы свою копию сессии.
            builder.Register<NgoDirectory>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<NgoSession>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<NgoStack>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterEntryPoint<StackAttachment>();
        }
    }
}
