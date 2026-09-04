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
        [SerializeField] private PauseView pauseView;
        [SerializeField] private NetworkStackDefinition[] stacks;
        [SerializeField] private ArenaDefinition[] arenas;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent<IStackSelectView>(stackSelectView);
            builder.RegisterComponent<ILobbyView>(lobbyView);
            builder.RegisterComponent<IHudMessages>(hudView);
            builder.RegisterComponent<IPauseView>(pauseView);
            builder.RegisterInstance(stacks);
            builder.RegisterInstance(arenas);
            /// AsImplementedInterfaces тут не нужен: RegisterEntryPoint делает его сам,
            /// а повторный контракт роняет сборку конфликтом типов реализации.
            builder.RegisterEntryPoint<StackFlow>().AsSelf();
            builder.RegisterEntryPoint<StackSelectPresenter>();
        }
    }
}
