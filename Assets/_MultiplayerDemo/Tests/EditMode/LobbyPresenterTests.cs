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

            public Observable<IReadOnlyList<HostEntry>> Hosts => _hosts;

            public FakeBrowser(List<string> log)
            {
                _log = log;
            }

            public void Dispose() => _hosts.Dispose();

            public void StartBrowsing() => Started = true;

            public void StopBrowsing() => Started = false;

            public void Advertise(string hostName, int players, int maxPlayers)
            {
                AdvertisedName = hostName;
                _log.Add("advertise");
            }

            public void Emit(params HostEntry[] hosts) => _hosts.Value = hosts;
        }

        private sealed class FakeSession : ISessionControl
        {
            private readonly ReactiveProperty<SessionState> _state = new(new SessionState(SessionPhase.Idle));
            private readonly List<string> _log;

            public string LastJoinToken;
            public bool Left;

            public ReadOnlyReactiveProperty<SessionState> State => _state;

            public FakeSession(List<string> log)
            {
                _log = log;
            }

            public UniTask StartHostAsync(string playerName, CancellationToken token)
            {
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

            public FakeArena(List<string> log)
            {
                _log = log;
            }

            public UniTask<ISpawnPointRegistry> LoadAsync(CancellationToken token)
            {
                Loads++;
                _log.Add("arena");
                return UniTask.FromResult<ISpawnPointRegistry>(null);
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

            public FakeSpawner(List<string> log)
            {
                _log = log;
            }

            public void UsePoints(ISpawnPointRegistry points) => _log.Add("points");

            public void SpawnItems() => _log.Add("items");
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

            public IReadOnlyList<HostEntry> Shown = Array.Empty<HostEntry>();
            public string Hint;
            public bool Visible;

            public Observable<string> HostRequested => _host;
            public Observable<HostEntry> JoinRequested => _join;

            public void Show() => Visible = true;

            public void Hide() => Visible = false;

            public void ShowHosts(IReadOnlyList<HostEntry> hosts) => Shown = hosts;

            public void SetEmptyHint(string text) => Hint = text;

            public void ClickHost(string name) => _host.OnNext(name);

            public void ClickJoin(HostEntry entry) => _join.OnNext(entry);
        }

        private sealed class Rig
        {
            public readonly List<string> Log = new();
            public readonly FakeBrowser Browser;
            public readonly FakeSession Session;
            public readonly FakeSpawner Spawner;
            public readonly FakeArena Arena;
            public readonly FakeHud Hud;
            public readonly FakeLobbyView View;
            public readonly DemoConfig Config;
            public readonly LobbyPresenter Presenter;

            public Rig()
            {
                Browser = new FakeBrowser(Log);
                Session = new FakeSession(Log);
                Spawner = new FakeSpawner(Log);
                Arena = new FakeArena(Log);
                Hud = new FakeHud();
                View = new FakeLobbyView();
                Config = ScriptableObject.CreateInstance<DemoConfig>();
                Presenter = new LobbyPresenter(Browser, Session, Spawner, Arena, Hud, View, Config);
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

            rig.View.ClickJoin(new HostEntry("Коля", 1, 4, "ngo", "192.168.0.5:7777"));

            Assert.AreEqual("192.168.0.5:7777", rig.Session.LastJoinToken);
        }

        [Test]
        public void FoundHostsReachTheView()
        {
            var rig = new Rig();
            rig.Presenter.Start();

            rig.Browser.Emit(new HostEntry("Коля", 1, 4, "ngo", "192.168.0.5:7777"));

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

            CollectionAssert.AreEqual(new[] { "arena", "points", "host", "items", "advertise" }, rig.Log);
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

        /// Пункт 8 чек-листа: закрытый хост не оставляет клиента в пустой арене.
        [Test]
        public void FailureReturnsPlayerToLobby()
        {
            var rig = new Rig();
            rig.Presenter.Start();
            rig.View.ClickJoin(new HostEntry("Коля", 1, 4, "ngo", "192.168.0.5:7777"));

            rig.Session.Fail("Хост недоступен или отключился");

            Assert.IsTrue(rig.Session.Left);
            Assert.AreEqual(1, rig.Arena.Unloads);
        }
    }
}
