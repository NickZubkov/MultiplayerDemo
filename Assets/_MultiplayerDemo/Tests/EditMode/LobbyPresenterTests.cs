using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.App;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace Game.Core.Tests
{
    /// Ни сцены, ни сети: всё, что презентер трогает, спрятано за портами.
    /// Общий журнал вызовов у подставок нужен ради теста порядка — именно порядок
    /// здесь легче всего сломать незаметно.
    public sealed class LobbyPresenterTests
    {
        private sealed class FakeBrowser : IHostBrowser, IHostAdvertiser
        {
            private readonly ReactiveProperty<IReadOnlyList<HostEntry>> _hosts = new(Array.Empty<HostEntry>());
            private readonly List<string> _log;

            public bool Started;
            public string AdvertisedName;
            public string AdvertisedArena;

            public Observable<IReadOnlyList<HostEntry>> Hosts => _hosts;

            public FakeBrowser(List<string> log)
            {
                _log = log;
            }

            public void Dispose() => _hosts.Dispose();

            public void StartBrowsing() => Started = true;

            public void StopBrowsing() => Started = false;

            public void Advertise(string hostName, int players, int maxPlayers, string arenaId)
            {
                AdvertisedName = hostName;
                AdvertisedArena = arenaId;
                _log.Add("advertise");
            }

            public void Emit(params HostEntry[] hosts) => _hosts.Value = hosts;
        }

        private sealed class FakeSession : ISessionControl
        {
            private readonly ReactiveProperty<SessionState> _state = new(new SessionState(SessionPhase.Idle));
            private readonly List<string> _log;

            public string LastJoinToken;
            public string HostedArena;
            public bool Left;

            public ReadOnlyReactiveProperty<SessionState> State => _state;

            public FakeSession(List<string> log)
            {
                _log = log;
            }

            public UniTask StartHostAsync(string playerName, string arenaId, CancellationToken token)
            {
                HostedArena = arenaId;
                _log.Add("host");
                _state.Value = new SessionState(SessionPhase.Hosting);
                return UniTask.CompletedTask;
            }

            public UniTask JoinAsync(HostEntry entry, CancellationToken token)
            {
                LastJoinToken = entry.JoinToken;
                _state.Value = new SessionState(SessionPhase.Connecting);
                return UniTask.CompletedTask;
            }

            public UniTask LeaveAsync()
            {
                Left = true;
                _state.Value = new SessionState(SessionPhase.Idle);
                return UniTask.CompletedTask;
            }

            public void Fail(string reason) => _state.Value = new SessionState(SessionPhase.Failed, reason);
        }

        private sealed class FakeArena : IArenaLoader
        {
            private readonly List<string> _log;

            public int Loads;
            public int Unloads;
            public ArenaDefinition LoadedArena;

            public FakeArena(List<string> log)
            {
                _log = log;
            }

            public UniTask LoadAsync(ArenaDefinition arena, CancellationToken token)
            {
                Loads++;
                LoadedArena = arena;
                _log.Add("arena");
                return UniTask.CompletedTask;
            }

            public UniTask UnloadAsync()
            {
                Unloads++;
                _log.Add("unload");
                return UniTask.CompletedTask;
            }
        }

        private sealed class FakeSpawner : IWorldSpawner
        {
            private readonly List<string> _log;

            public bool PointsTaken;

            public FakeSpawner(List<string> log)
            {
                _log = log;
            }

            /// Презентер этот метод больше не зовёт: точки спавнеру отдаёт ArenaScope
            /// в момент загрузки сцены. В журнал не пишем — иначе тест порядка ждал бы
            /// вызова, которого в этом месте потока уже нет.
            public void UsePoints(ISpawnPointRegistry points) => PointsTaken = true;

            public void SpawnItems() => _log.Add("items");
        }

        private sealed class FakePauseView : IPauseView
        {
            private readonly Subject<Unit> _toggle = new();
            private readonly Subject<Unit> _resume = new();
            private readonly Subject<Unit> _exit = new();

            public bool Visible;
            public bool CursorCapturedOnHide;

            public Observable<Unit> ToggleRequested => _toggle;
            public Observable<Unit> ResumeRequested => _resume;
            public Observable<Unit> ExitRequested => _exit;

            public void Show() => Visible = true;

            public void Hide(bool captureCursor)
            {
                Visible = false;
                CursorCapturedOnHide = captureCursor;
            }

            public void PressCancel() => _toggle.OnNext(Unit.Default);

            public void ClickResume() => _resume.OnNext(Unit.Default);

            public void ClickExit() => _exit.OnNext(Unit.Default);
        }

        private sealed class FakeStackFlow : IStackFlow
        {
            private readonly Subject<Unit> _back = new();

            public bool BackRequested;

            public Observable<Unit> BackToSelect => _back;

            public UniTask LoadAsync(NetworkStackDefinition stack, CancellationToken token) => UniTask.CompletedTask;

            public void RequestBackToSelect() => BackRequested = true;
        }

        /// NetworkStackDefinition абстрактен — наследников заводят стеки, а тестам
        /// достаточно пустого: нужны только значения полей по умолчанию.
        private sealed class FakeStack : NetworkStackDefinition
        {
        }

        private sealed class FakeHud : IHudMessages
        {
            public string Last;

            public void Show(string message) => Last = message;
        }

        private sealed class FakeLobbyView : ILobbyView
        {
            private readonly Subject<string> _host = new();
            private readonly Subject<HostEntry> _join = new();

            private readonly Subject<ArenaDefinition> _arenaChosen = new();
            private readonly Subject<Unit> _backToStacks = new();

            public IReadOnlyList<HostEntry> Shown = Array.Empty<HostEntry>();
            public IReadOnlyList<ArenaDefinition> ShownArenas = Array.Empty<ArenaDefinition>();
            public ArenaDefinition MarkedArena;
            public string Hint;
            public string ManualHint;
            public bool Visible;

            public Observable<string> HostRequested => _host;
            public Observable<HostEntry> JoinRequested => _join;
            public Observable<ArenaDefinition> ArenaChosen => _arenaChosen;
            public Observable<Unit> BackToStacksRequested => _backToStacks;

            public void Show() => Visible = true;

            public void Hide() => Visible = false;

            public void ShowHosts(IReadOnlyList<HostEntry> hosts) => Shown = hosts;

            public void ShowArenas(IReadOnlyList<ArenaDefinition> arenas) => ShownArenas = arenas;

            public void MarkArena(ArenaDefinition arena) => MarkedArena = arena;

            public void SetEmptyHint(string text) => Hint = text;

            public void SetManualHint(string text) => ManualHint = text;

            public void ClickHost(string name) => _host.OnNext(name);

            public void ClickJoin(HostEntry entry) => _join.OnNext(entry);

            public void ChooseArena(ArenaDefinition arena) => _arenaChosen.OnNext(arena);

            public void ClickBackToStacks() => _backToStacks.OnNext(Unit.Default);
        }

        private sealed class Rig
        {
            public readonly List<string> Log = new();
            public readonly FakeBrowser Browser;
            public readonly FakeSession Session;
            public readonly FakeSpawner Spawner;
            public readonly FakeArena ArenaFlow;
            public readonly FakeHud Hud;
            public readonly FakeLobbyView View;
            public readonly FakePauseView Pause;
            public readonly FakeStackFlow StackFlow;
            public readonly DemoConfig Config;
            public readonly NetworkStackDefinition Stack;
            public readonly ArenaDefinition Arena;
            public readonly ArenaDefinition SecondArena;
            public readonly LobbyPresenter Presenter;

            public Rig()
            {
                Browser = new FakeBrowser(Log);
                Session = new FakeSession(Log);
                Spawner = new FakeSpawner(Log);
                ArenaFlow = new FakeArena(Log);
                Hud = new FakeHud();
                View = new FakeLobbyView();
                Pause = new FakePauseView();
                StackFlow = new FakeStackFlow();
                Config = ScriptableObject.CreateInstance<DemoConfig>();
                Stack = ScriptableObject.CreateInstance<FakeStack>();
                Arena = ScriptableObject.CreateInstance<ArenaDefinition>();
                SecondArena = ScriptableObject.CreateInstance<ArenaDefinition>();
                Presenter = new LobbyPresenter(Browser, Session, Spawner, ArenaFlow, StackFlow, Hud, View,
                    Pause, Config, Stack, new[] { Arena, SecondArena });
            }
        }

        [Test]
        public void FailureReasonReachesPlayer()
        {
            var rig = new Rig();
            rig.Presenter.Start();

            rig.Session.Fail("Хост недоступен или отключился");

            Assert.AreEqual("Хост недоступен или отключился", rig.Hud.Last);
        }

        [Test]
        public void JoinPassesTokenUnchanged()
        {
            var rig = new Rig();
            rig.Presenter.Start();

            rig.View.ClickJoin(new HostEntry("Коля", 1, 4, "ngo", "192.168.0.5:7777", "box"));

            Assert.AreEqual("192.168.0.5:7777", rig.Session.LastJoinToken);
        }

        [Test]
        public void FoundHostsReachTheView()
        {
            var rig = new Rig();
            rig.Presenter.Start();

            rig.Browser.Emit(new HostEntry("Коля", 1, 4, "ngo", "192.168.0.5:7777", "box"));

            Assert.AreEqual(1, rig.View.Shown.Count);
        }

        [Test]
        public void BrowsingStartsWithPresenter()
        {
            var rig = new Rig();
            rig.Presenter.Start();
            Assert.IsTrue(rig.Browser.Started);
        }

        /// Арена — до старта сессии: NGO спавнит игрока прямо внутри StartHost,
        /// и без загруженного пола капсула улетает вниз (проверено в задаче 7).
        [Test]
        public void ArenaLoadsBeforeHostStarts()
        {
            var rig = new Rig();
            rig.Presenter.Start();

            rig.View.ClickHost("Коля");

            CollectionAssert.AreEqual(new[] { "arena", "host", "items", "advertise" }, rig.Log);
        }

        [Test]
        public void LobbyHidesInGameAndReturnsAfterFailure()
        {
            var rig = new Rig();
            rig.Presenter.Start();
            Assert.IsTrue(rig.View.Visible);

            rig.View.ClickHost("Коля");
            Assert.IsFalse(rig.View.Visible);

            rig.Session.Fail("Хост недоступен или отключился");
            Assert.IsTrue(rig.View.Visible);
        }

        /// Уровень уходит обоими путями сразу, и это не дублирование: LAN-стеки объявят
        /// его маяком после старта, а облачным он нужен в самом StartHostAsync — Photon
        /// принимает свойства сессии только при создании комнаты.
        [Test]
        public void HostAdvertisesSelectedArena()
        {
            var rig = new Rig();
            rig.Presenter.Start();

            rig.View.ClickHost("Коля");

            Assert.AreEqual(rig.Arena, rig.ArenaFlow.LoadedArena);
            Assert.AreEqual("box", rig.Browser.AdvertisedArena);
            Assert.AreEqual("box", rig.Session.HostedArena);
        }

        /// Уровень хоста может быть из чужой сборки — грузить нечего, и молчать нельзя.
        [Test]
        public void UnknownArenaOfHostIsRefusedWithMessage()
        {
            var rig = new Rig();
            rig.Presenter.Start();

            rig.View.ClickJoin(new HostEntry("Коля", 1, 4, "ngo", "192.168.0.5:7777", "ghost"));

            Assert.IsNull(rig.Session.LastJoinToken);
            Assert.AreEqual("Хост играет на уровне, которого нет в этой сборке", rig.Hud.Last);
            Assert.AreEqual(0, rig.ArenaFlow.Loads);
        }

        [Test]
        public void ChosenArenaIsMarkedAndUsedForHosting()
        {
            var rig = new Rig();
            rig.Presenter.Start();
            Assert.AreEqual(rig.Arena, rig.View.MarkedArena);

            rig.View.ChooseArena(rig.SecondArena);
            rig.View.ClickHost("Коля");

            Assert.AreEqual(rig.SecondArena, rig.View.MarkedArena);
            Assert.AreEqual(rig.SecondArena, rig.ArenaFlow.LoadedArena);
        }

        /// Подпись поля ручного ввода приходит из описания стека: у Fusion там имя сессии.
        [Test]
        public void ManualHintComesFromStack()
        {
            var rig = new Rig();
            rig.Presenter.Start();

            Assert.AreEqual(rig.Stack.ManualEntryHint, rig.View.ManualHint);
        }

        /// В лобби пауза бессмысленна: выходить неоткуда, а курсор и так свободен.
        [Test]
        public void PauseOpensOnlyInMatch()
        {
            var rig = new Rig();
            rig.Presenter.Start();

            rig.Pause.PressCancel();
            Assert.IsFalse(rig.Pause.Visible);

            rig.View.ClickHost("Коля");
            rig.Pause.PressCancel();

            Assert.IsTrue(rig.Pause.Visible);
        }

        [Test]
        public void ResumeClosesPauseAndReturnsCursorToMatch()
        {
            var rig = new Rig();
            rig.Presenter.Start();
            rig.View.ClickHost("Коля");
            rig.Pause.PressCancel();

            rig.Pause.ClickResume();

            Assert.IsFalse(rig.Pause.Visible);
            Assert.IsTrue(rig.Pause.CursorCapturedOnHide);
        }

        /// Выход из матча: сессия закрыта, арена выгружена, лобби на экране,
        /// курсор остаётся свободным — он нужен для кнопок лобби.
        [Test]
        public void ExitFromPauseReturnsToLobby()
        {
            var rig = new Rig();
            rig.Presenter.Start();
            rig.View.ClickHost("Коля");
            rig.Pause.PressCancel();

            rig.Pause.ClickExit();

            Assert.IsTrue(rig.Session.Left);
            Assert.AreEqual(1, rig.ArenaFlow.Unloads);
            Assert.IsTrue(rig.View.Visible);
            Assert.IsFalse(rig.Pause.CursorCapturedOnHide);
        }

        /// Сцену стека презентер не трогает: он живёт в ней самой и до конца выгрузки
        /// не дожил бы — просьба уходит в StackFlow из scope лобби.
        [Test]
        public void BackToStacksClosesMatchAndAsksFlow()
        {
            var rig = new Rig();
            rig.Presenter.Start();
            rig.View.ClickHost("Коля");

            rig.View.ClickBackToStacks();

            Assert.IsTrue(rig.Session.Left);
            Assert.AreEqual(1, rig.ArenaFlow.Unloads);
            Assert.IsTrue(rig.StackFlow.BackRequested);
        }

        /// Пункт 8 чек-листа: закрытый хост не оставляет клиента в пустой арене.
        [Test]
        public void FailureReturnsPlayerToLobby()
        {
            var rig = new Rig();
            rig.Presenter.Start();
            rig.View.ClickJoin(new HostEntry("Коля", 1, 4, "ngo", "192.168.0.5:7777", "box"));

            rig.Session.Fail("Хост недоступен или отключился");

            Assert.IsTrue(rig.Session.Left);
            Assert.AreEqual(1, rig.ArenaFlow.Unloads);
        }
    }
}
