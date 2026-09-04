using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Gameplay;
using UnityEngine;

namespace Game.App
{
    /// Арена одна на все стеки и про сеть не знает: маркеры собирает ArenaScopeInstaller,
    /// а кто и когда расставит по ним игроков и ящики — дело стека.
    public sealed class ArenaLoader : IArenaLoader
    {
        /// Помним загруженную сцену, а не имя из настроек: выгружать нужно ровно ту,
        /// которую грузили, даже если в лобби уже отметили другой уровень.
        private string _loadedScene;

        public async UniTask<ISpawnPointRegistry> LoadAsync(ArenaDefinition arena, CancellationToken token)
        {
            await SceneFlow.LoadAdditiveAsync(arena.SceneName, token);
            _loadedScene = arena.SceneName;
            return Object.FindFirstObjectByType<ArenaScopeInstaller>();
        }

        public async UniTask UnloadAsync()
        {
            if (_loadedScene == null) return;

            await SceneFlow.UnloadAsync(_loadedScene);
            _loadedScene = null;
        }
    }
}
