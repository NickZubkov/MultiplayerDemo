using System;
using System.Collections.Generic;
using Game.Core;
using Game.Net;
using R3;
using VContainer.Unity;

namespace Game.UI
{
    public sealed class StackSelectPresenter : IScreen, IStackSelectPresenter, IStartable, IDisposable
    {
        private readonly IStackSelectView _view;
        private readonly NetworkStackDefinition[] _stacks;
        private readonly IStackFlow _flow;
        private readonly IMatchFlow _match;
        private readonly ReactiveProperty<bool> _wanted = new(false);

        private DisposableBag _subscriptions;

        public ScreenLayer Layer => ScreenLayer.Base;
        public bool CapturesInput => true;
        public bool NeedsCursor => true;
        public ReadOnlyReactiveProperty<bool> Wanted => _wanted;
        public IReadOnlyList<NetworkStackDefinition> Stacks => _stacks;

        public StackSelectPresenter(IStackSelectView view, NetworkStackDefinition[] stacks, IStackFlow flow,
            IMatchFlow match)
        {
            _view = view;
            _stacks = stacks;
            _flow = flow;
            _match = match;
        }

        /// AwaitOperation.Drop: пока сцена стека грузится, повторные нажатия игнорируются.
        public void Start()
        {
            _match.Phase
                .Subscribe(phase => _wanted.Value = phase == AppPhase.SelectingStack)
                .AddTo(ref _subscriptions);
            _view.StackChosen
                .SubscribeAwait((stack, token) => _flow.LoadAsync(stack, token), AwaitOperation.Drop)
                .AddTo(ref _subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            _wanted.Dispose();
        }

        public void Show() => _view.Show(_stacks);

        public void Hide() => _view.Hide();
    }
}
