using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Gameplay;
using Game.Net;
using UnityEngine.SceneManagement;
using VContainer;

namespace Game.App
{
    /// Живёт в BootstrapScope, а не в scope стека: арена теперь соседка стека, а не его дочь
    /// (спека § 4). После загрузки отдаёт мир арены — его собрал ArenaScope сцены.
    public sealed class ArenaLoader : IArenaLoader
    {
        private string _loadedScene;
        private UniTask _loading = UniTask.CompletedTask;

        /// SceneManager загрузку не отменяет: токен рвёт только продолжение, а сцена догрузится.
        /// Поэтому имя запоминаем до await, а саму загрузку — чтобы выгрузка её дождалась.
        public async UniTask<INetWorld> LoadAsync(ArenaDefinition arena, CancellationToken token)
        {
            _loadedScene = arena.SceneName;
            _loading = SceneFlow.LoadAdditiveAsync(arena.SceneName, CancellationToken.None).Preserve();

            await _loading.AttachExternalCancellation(token);
            return WorldOf(arena.SceneName);
        }

        public async UniTask UnloadAsync()
        {
            if (_loadedScene == null) return;

            var scene = _loadedScene;
            _loadedScene = null;

            await _loading;
            await SceneFlow.UnloadAsync(scene);
        }

        private static INetWorld WorldOf(string sceneName)
        {
            foreach (var root in SceneManager.GetSceneByName(sceneName).GetRootGameObjects())
            {
                if (root.TryGetComponent<ArenaScope>(out var scope)) return scope.Container.Resolve<INetWorld>();
            }

            throw new InvalidOperationException($"В сцене {sceneName} нет ArenaScope на корневом объекте");
        }
    }
}
