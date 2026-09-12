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
        [SerializeField] private StackSelectView _stackSelectView;
        [SerializeField] private LobbyView _lobbyView;
        [SerializeField] private HudMessagesView _hudView;
        [SerializeField] private PauseView _pauseView;
        [SerializeField] private NetworkStackDefinition[] _stacks;
        [SerializeField] private ArenaDefinition[] _arenas;

        /// Виды будим до сборки графа. Панель, выключенную в сцене галочкой, Unity
        /// обходит стороной: Awake у её компонентов не зовётся вовсе, и вид остаётся без
        /// подписок — у паузы так пропадает и Cancel, то есть открыть её становится
        /// нечем. Начальную видимость каждый вид ставит себе сам в своём Awake, поэтому
        /// после пробуждения экран выглядит одинаково независимо от того, в каком
        /// состоянии сцену сохранили после вёрстки.
        protected override void Awake()
        {
            _stackSelectView.gameObject.SetActive(true);
            _lobbyView.gameObject.SetActive(true);
            _hudView.gameObject.SetActive(true);
            _pauseView.gameObject.SetActive(true);

            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent<IStackSelectView>(_stackSelectView);
            builder.RegisterComponent<ILobbyView>(_lobbyView);
            builder.RegisterComponent<IHudMessages>(_hudView);
            builder.RegisterComponent<IPauseView>(_pauseView);
            builder.RegisterInstance(_stacks);
            builder.RegisterInstance(_arenas);
            /// AsImplementedInterfaces тут не нужен: RegisterEntryPoint делает его сам,
            /// а повторный контракт роняет сборку конфликтом типов реализации.
            builder.RegisterEntryPoint<StackFlow>().AsSelf();
            builder.RegisterEntryPoint<StackSelectPresenter>();
        }
    }
}
