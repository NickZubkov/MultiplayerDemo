using Game.Core;
using Game.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.App
{
    /// Scope сцены лобби: «глупые» View и выбор стека.
    /// Сервисы конкретного стека появятся в дочернем scope его сцены.
    public sealed class BootstrapScope : LifetimeScope
    {
        [SerializeField] private StackSelectView stackSelectView;
        [SerializeField] private LobbyView lobbyView;
        [SerializeField] private HudMessagesView hudView;
        [SerializeField] private NetworkStackDefinition[] stacks;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent<IStackSelectView>(stackSelectView);
            builder.RegisterComponent<ILobbyView>(lobbyView);
            builder.RegisterComponent<IHudMessages>(hudView);
            builder.RegisterInstance(stacks);
            builder.RegisterEntryPoint<StackSelectPresenter>();
        }
    }
}
