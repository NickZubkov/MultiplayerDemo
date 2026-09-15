using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Net;
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
        private readonly IStackFlow _flow;
        private readonly CancellationTokenSource _lifetime = new();

        private DisposableBag _subscriptions;

        public StackSelectPresenter(IStackSelectView view, NetworkStackDefinition[] stacks, IStackFlow flow)
        {
            _view = view;
            _stacks = stacks;
            _flow = flow;
        }

        public void Start()
        {
            _view.Show(_stacks);
            _view.StackChosen
                 .SubscribeAwait((stack, token) => LoadAsync(stack, token), AwaitOperation.Drop)
                 .AddTo(ref _subscriptions);

            /// Из лобби можно вернуться сюда: сцену стека к этому моменту уже выгрузил
            /// StackFlow, нам остаётся показать экран.
            _flow.BackToSelect
                 .Subscribe(_ => _view.Show(_stacks))
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
        private async UniTask LoadAsync(NetworkStackDefinition stack, CancellationToken token)
        {
            await _flow.LoadAsync(stack, token);
            _view.Hide();
        }
    }
}
