using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Net;

namespace Game.App
{
    /// Живёт в BootstrapScope и потому переживает и сцену стека, и сцену арены — это и есть
    /// причина, по которой сцены грузит он, а не презентеры. Родителя сценам больше не
    /// подставляет: у сцен стеков и арен он один и тот же и проставлен в инспекторе.
    public sealed class StackFlow : IStackFlow
    {
        private string _loadedScene;
        private UniTask _loading = UniTask.CompletedTask;

        /// Имя сцены запоминаем до await: SceneManager загрузку не отменяет, токен рвёт
        /// только продолжение — а сцена догрузится, и выгрузить её обязаны (И-12).
        public async UniTask LoadAsync(NetworkStackDefinition stack, CancellationToken token)
        {
            _loadedScene = stack.ManagerScene;
            _loading = SceneFlow.LoadAdditiveAsync(stack.ManagerScene, CancellationToken.None).Preserve();

            await _loading.AttachExternalCancellation(token);
        }

        public async UniTask UnloadAsync()
        {
            if (_loadedScene == null) return;

            var scene = _loadedScene;
            _loadedScene = null;

            await _loading;
            await SceneFlow.UnloadAsync(scene);
        }
    }
}
