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

        /// Виды будим до сборки графа. Панель, выключенную в сцене галочкой, Unity
        /// обходит стороной: Awake у её компонентов не зовётся вовсе, и вид остаётся без
        /// подписок — у паузы так пропадает и Cancel, то есть открыть её становится
        /// нечем. Начальную видимость каждый вид ставит себе сам в своём Awake, поэтому
        /// после пробуждения экран выглядит одинаково независимо от того, в каком
        /// состоянии сцену сохранили после вёрстки.
        protected override void Awake()
        {
            stackSelectView.gameObject.SetActive(true);
            lobbyView.gameObject.SetActive(true);
            hudView.gameObject.SetActive(true);
            pauseView.gameObject.SetActive(true);

            base.Awake();
        }

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
