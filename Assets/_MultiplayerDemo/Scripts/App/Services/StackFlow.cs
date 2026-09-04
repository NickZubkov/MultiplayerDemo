using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using R3;
using VContainer.Unity;

namespace Game.App
{
    /// Живёт в scope сцены Bootstrap и потому переживает и сцену стека, и сцену арены —
    /// это и есть причина, по которой сцены грузит он, а не презентеры.
    public sealed class StackFlow : IStackFlow, IStartable, IDisposable
    {
        private readonly LifetimeScope _lobbyScope;
        private readonly Subject<Unit> _backRequested = new();
        private readonly Subject<Unit> _backToSelect = new();

        private DisposableBag _subscriptions;
        private string _loadedScene;

        public Observable<Unit> BackToSelect => _backToSelect;

        public StackFlow(LifetimeScope lobbyScope)
        {
            _lobbyScope = lobbyScope;
        }

        /// Просьбу вернуться исполняем не в вызове, а здесь, в собственной подписке:
        /// вызывающий презентер живёт в выгружаемой сцене и до конца операции не доживёт.
        public void Start()
        {
            _backRequested
                .SubscribeAwait((_, _) => UnloadAsync(), AwaitOperation.Drop)
                .AddTo(ref _subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            _backRequested.Dispose();
            _backToSelect.Dispose();
        }

        public async UniTask LoadAsync(NetworkStackDefinition stack, CancellationToken token)
        {
            /// Область действия обязана закрыться сразу после загрузки: иначе подставленный
            /// родитель останется в глобальном стеке VContainer и достанется первому же
            /// следующему scope.
            using (LifetimeScope.EnqueueParent(_lobbyScope))
            {
                await SceneFlow.LoadAdditiveAsync(stack.ManagerScene, token);
            }

            _loadedScene = stack.ManagerScene;
        }

        public void RequestBackToSelect() => _backRequested.OnNext(Unit.Default);

        private async UniTask UnloadAsync()
        {
            if (_loadedScene != null)
            {
                await SceneFlow.UnloadAsync(_loadedScene);
                _loadedScene = null;
            }

            _backToSelect.OnNext(Unit.Default);
        }
    }
}
