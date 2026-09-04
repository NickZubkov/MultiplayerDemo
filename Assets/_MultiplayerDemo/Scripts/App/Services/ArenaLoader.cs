using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using VContainer.Unity;

namespace Game.App
{
    /// Арена одна на все стеки и про сеть не знает: маркеры собирает ArenaSpawnPoints,
    /// регистрирует их ArenaScope, а кто расставит по ним игроков и ящики — дело стека.
    ///
    /// Загрузчику остаётся единственная обязанность — накрыть сцену родителем, то есть
    /// scope того стека, который её грузит: без этого ArenaScope не найдёт IWorldSpawner.
    /// Сам scope приходит из встроенной регистрации VContainer (последняя строка
    /// LifetimeScope.InstallTo), поэтому в Configure его регистрировать не надо.
    public sealed class ArenaLoader : IArenaLoader
    {
        private readonly LifetimeScope _stackScope;

        /// Помним загруженную сцену, а не имя из настроек: выгружать нужно ровно ту,
        /// которую грузили, даже если в лобби уже отметили другой уровень.
        private string _loadedScene;

        public ArenaLoader(LifetimeScope stackScope)
        {
            _stackScope = stackScope;
        }

        public async UniTask LoadAsync(ArenaDefinition arena, CancellationToken token)
        {
            /// Область действия закрывается сразу после загрузки: иначе подставленный
            /// родитель останется в глобальном стеке VContainer и достанется первому же
            /// следующему scope — та же оговорка, что в StackSelectPresenter.
            using (LifetimeScope.EnqueueParent(_stackScope))
            {
                await SceneFlow.LoadAdditiveAsync(arena.SceneName, token);
            }

            _loadedScene = arena.SceneName;
        }

        public async UniTask UnloadAsync()
        {
            if (_loadedScene == null) return;

            await SceneFlow.UnloadAsync(_loadedScene);
            _loadedScene = null;
        }
    }
}
