using Game.Core;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay
{
    /// Scope сцены арены. Арена по-прежнему не знает про сеть — только про два порта
    /// из Game.Core, и одна и та же сцена годится всем трём стекам.
    ///
    /// Родитель приходит снаружи, через EnqueueParent в ArenaLoader, и поле parentReference
    /// здесь намеренно пустое: родитель у арены разный — scope того стека, который её грузит.
    public sealed class ArenaScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            /// Ищет только в своей сцене, включая выключенные объекты, и падает с внятным
            /// сообщением, если точек нет: арена без маркеров — сломанная сцена, а не норма.
            builder.RegisterComponentInHierarchy<ArenaSpawnPoints>().AsImplementedInterfaces();

            /// Резолв идёт вверх, поэтому точки не забирают — их отдают:
            /// IWorldSpawner живёт в scope стека, то есть в родителе.
            builder.RegisterBuildCallback(container =>
                container.Resolve<IWorldSpawner>().UsePoints(container.Resolve<ISpawnPointRegistry>()));
        }
    }
}
