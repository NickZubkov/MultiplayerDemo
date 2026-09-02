using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using R3;
using VContainer.Unity;

namespace Game.App
{
    /// Сцена стека грузится сразу при выборе, а не при старте сессии:
    /// NetworkManager и Runner должны существовать до StartHost,
    /// а браузер хостов нужен ещё раньше — в лобби.
    public sealed class StackSelectPresenter : IStartable, IDisposable
    {
        private readonly IStackSelectView _view;
        private readonly NetworkStackDefinition[] _stacks;
        private readonly CancellationTokenSource _lifetime = new();

        private DisposableBag _subscriptions;

        public StackSelectPresenter(IStackSelectView view, NetworkStackDefinition[] stacks)
        {
            _view = view;
            _stacks = stacks;
        }

        public void Start()
        {
            _view.Show(_stacks);
            _view.StackChosen
                 .SubscribeAwait((stack, token) => LoadAsync(stack, token), AwaitOperation.Drop)
                 .AddTo(ref _subscriptions);
        }

        public void Dispose()
        {
            _lifetime.Cancel();
            _lifetime.Dispose();
            _subscriptions.Dispose();
        }

        /// AwaitOperation.Drop: пока сцена грузится, повторные нажатия игнорируются —
        /// то, что в корутинной версии пришлось бы городить флагом.
        ///
        /// EnqueueParent возвращает ParentOverrideScope и обязан жить только на время
        /// загрузки: иначе подставленный родитель остаётся в глобальном стеке VContainer
        /// и достаётся первому же следующему scope, который создадут.
        private async UniTask LoadAsync(NetworkStackDefinition stack, CancellationToken token)
        {
            using (LifetimeScope.EnqueueParent(LifetimeScope.Find<BootstrapScope>()))
            {
                await SceneFlow.LoadAdditiveAsync(stack.ManagerScene, token);
            }

            _view.Hide();
        }
    }
}
