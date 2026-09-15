using Game.Core;
using Game.Gameplay;
using Game.Net;
using Game.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.App
{
    /// Scope сцены Bootstrap: всё, что живёт дольше матча, — сценарий матча, сервис экранов,
    /// презентеры, виды, загрузчики сцен, гнездо стека. Сервисы конкретного стека появятся
    /// в соседнем scope его сцены.
    public sealed class BootstrapScope : LifetimeScope
    {
        [SerializeField] private StackSelectView _stackSelectView;
        [SerializeField] private LobbyView _lobbyView;
        [SerializeField] private HudMessagesView _hudView;
        [SerializeField] private PauseView _pauseView;
        [SerializeField] private NetworkStackDefinition[] _stacks;
        [SerializeField] private ArenaDefinition[] _arenas;
        [SerializeField] private PlayerInputBindings _inputBindings;
        [SerializeField] private AvatarControllerProvider _controllerProvider;

        /// Виды будим до сборки графа. Панель, выключенную в сцене галочкой, Unity
        /// обходит стороной: Awake у её компонентов не зовётся вовсе, и вид остаётся без
        /// подписок — у паузы так пропадает и Cancel, то есть открыть её становится
        /// нечем. Начальную видимость задаёт UiService.Start, поэтому после пробуждения
        /// экраны выглядят одинаково независимо от того, в каком состоянии сцену
        /// сохранили после вёрстки.
        protected override void Awake()
        {
            _stackSelectView.gameObject.SetActive(true);
            _lobbyView.gameObject.SetActive(true);
            _hudView.gameObject.SetActive(true);
            _pauseView.gameObject.SetActive(true);

            base.Awake();
        }

        /// При выходе из Play Mode Unity рушит объекты сцены в непредсказуемом порядке: виды
        /// умирают раньше, чем scope успевает разобрать граф, а scope стека, уходя из гнезда,
        /// поднимает весь конвейер «фаза → презентеры → экраны» поверх уже снесённых видов —
        /// и тот падает на первом же обращении к ним. OnApplicationQuit приходит до первого
        /// OnDestroy: разбираем граф здесь, и реагировать на поздний уход стека уже некому.
        /// Повторный DisposeCore из OnDestroy безвреден — контейнера к тому моменту нет.
        private void OnApplicationQuit() => DisposeCore();

        protected override void Configure(IContainerBuilder builder)
        {
            /// Виды — широким контрактом (И-13): появись у вида IDisposable, контейнер это увидит.
            builder.RegisterComponent(_stackSelectView).AsImplementedInterfaces().AsSelf();
            builder.RegisterComponent(_lobbyView).AsImplementedInterfaces().AsSelf();
            builder.RegisterComponent(_hudView).AsImplementedInterfaces().AsSelf();
            builder.RegisterComponent(_pauseView).AsImplementedInterfaces().AsSelf();

            builder.RegisterInstance(_stacks);
            builder.RegisterInstance(_arenas);
            builder.RegisterInstance(_inputBindings);

            /// Провайдер регистрируется базовым типом, как описание стека: потребителю
            /// нужен контракт, а не конкретный ассет.
            builder.RegisterInstance<AvatarControllerProvider>(_controllerProvider);

            builder.Register<InputGate>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ProjectActionsInput>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<UnityCursor>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<NetworkSlot>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<StackFlow>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ArenaLoader>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();

            /// Порядок точек входа — порядок их Start: сценарий раньше презентеров (они читают
            /// его фазу), сервис экранов — последним (он читает их желания).
            ///
            /// AsImplementedInterfaces тут не нужен: RegisterEntryPoint делает его сам,
            /// а повторный контракт роняет сборку конфликтом типов реализации.
            builder.RegisterEntryPoint<MatchFlow>();
            builder.RegisterEntryPoint<StackSelectPresenter>();
            builder.RegisterEntryPoint<LobbyPresenter>();
            builder.RegisterEntryPoint<PausePresenter>();
            builder.RegisterEntryPoint<HudPresenter>();
            builder.RegisterEntryPoint<UiService>();
        }
    }
}
