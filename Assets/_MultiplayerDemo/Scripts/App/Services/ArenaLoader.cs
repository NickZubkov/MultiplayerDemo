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
        private const string ArenaScene = "Arena";

        public async UniTask<ISpawnPointRegistry> LoadAsync(CancellationToken token)
        {
            await SceneFlow.LoadAdditiveAsync(ArenaScene, token);
            return Object.FindFirstObjectByType<ArenaScopeInstaller>();
        }

        public UniTask UnloadAsync() => SceneFlow.UnloadAsync(ArenaScene);
    }
}
