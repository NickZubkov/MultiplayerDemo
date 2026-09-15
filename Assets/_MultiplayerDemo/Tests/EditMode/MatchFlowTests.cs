using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.App;
using Game.Core;
using Game.Net;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace Game.Tests
{
    /// Сценарий матча целиком: порядок «арена до сессии», мир параметром, отказы и разбор
    /// снизу вверх. Все подставки отвечают синхронно, поэтому результат проверяется сразу
    /// после Forget().
    public sealed class MatchFlowTests
    {
        private sealed class FakeWorld : INetWorld
        {
            public IEntityFactory Factory => null;
            public IReadOnlyList<NetEntity> SceneEntities { get; } = Array.Empty<NetEntity>();
            public IReadOnlyList<Placement> Placements { get; } = Array.Empty<Placement>();

            public Pose AvatarPose(PlayerId player) => Pose.identity;
        }

        private sealed class FakeArena : IArenaLoader
        {
            public readonly FakeWorld World = new();

            private readonly List<string> _log;

            public ArenaDefinition Loaded;
            public bool CancelNextLoad;

            public FakeArena(List<string> log)
            {
                _log = log;
            }

            public UniTask<INetWorld> LoadAsync(ArenaDefinition arena, CancellationToken token)
            {
                _log.Add("arena");
                Loaded = arena;

                if (CancelNextLoad)
                {
                    CancelNextLoad = false;
                    throw new OperationCanceledException();
                }

                return UniTask.FromResult<INetWorld>(World);
            }

            public UniTask UnloadAsync()
            {
                _log.Add("unload");
                return UniTask.CompletedTask;
            }
        }

        private sealed class FakeSession : INetSession
        {
            private readonly ReactiveProperty<SessionState> _state = new(new SessionState(SessionPhase.Idle));
            private readonly List<string> _log;
            private readonly PlayerId[] _players = { new(0) };

            public SessionSettings Settings;
            public INetWorld World;
            public HostEntry JoinedEntry;

            public ReadOnlyReactiveProperty<SessionState> State => _state;
            public PlayerId LocalPlayer => _players[0];
            public bool IsJudge => true;
            public IReadOnlyCollection<PlayerId> Players => _players;
            public Observable<PlayerId> PlayerJoined => Observable.Empty<PlayerId>();
            public Observable<PlayerId> PlayerLeft => Observable.Empty<PlayerId>();

            public FakeSession(List<string> log)
            {
                _log = log;
            }

            public UniTask StartHostAsync(SessionSettings settings, INetWorld world, CancellationToken token)
            {
                _log.Add("host");
                Settings = settings;
                World = world;
                _state.Value = new SessionState(SessionPhase.Hosting);
                return UniTask.CompletedTask;
            }

            public UniTask JoinAsync(HostEntry entry, INetWorld world, CancellationToken token)
            {
                _log.Add("join");
                JoinedEntry = entry;
                World = world;
                _state.Value = new SessionState(SessionPhase.Connecting);
                return UniTask.CompletedTask;
            }

            public UniTask LeaveAsync()
            {
                _log.Add("leave");
                _state.Value = new SessionState(SessionPhase.Idle);
                return UniTask.CompletedTask;
            }

            public void Fail(string reason) => _state.Value = new SessionState(SessionPhase.Failed, reason);
        }

        private sealed class FakeStackFlow : IStackFlow
        {
            private readonly List<string> _log;
            private readonly NetworkSlot _slot;

            /// Сцену стека подставка не грузит, но снять стек из гнезда обязана: на этом
            /// держится возврат в фазу выбора стека.
            public INetworkStack Stack;

            public FakeStackFlow(List<string> log, NetworkSlot slot)
            {
                _log = log;
                _slot = slot;
            }

            public UniTask LoadAsync(NetworkStackDefinition stack, CancellationToken token) => UniTask.CompletedTask;

            public UniTask UnloadAsync()
            {
                _log.Add("stack-unload");
                _slot.Detach(Stack);
                return UniTask.CompletedTask;
            }
        }

        private sealed class Rig : IDisposable
        {
            public readonly List<string> Log = new();
            public readonly NetworkSlot Slot = new();
            public readonly FakeArena Arena;
            public readonly FakeSession Session;
            public readonly FakeDirectory Directory = new();
            public readonly FakeStackDefinition Definition;
            public readonly FakeNetworkStack Stack;
            public readonly MatchFlow Flow;
            public readonly ArenaDefinition Box;
            public readonly ArenaDefinition Yard;

            private readonly DemoConfig _config;

            public Rig(bool attachStack = true)
            {
                Box = ScriptableObject.CreateInstance<ArenaDefinition>();
                Yard = ScriptableObject.CreateInstance<ArenaDefinition>();

                /// Поля описаний приватные, а два разных уровня тестам нужны: имена ключей
                /// JSON — это имена полей, с подчёркиванием.
                JsonUtility.FromJsonOverwrite("{\"_arenaId\":\"yard\",\"_sceneName\":\"Arena_Yard\"}", Yard);

                Definition = ScriptableObject.CreateInstance<FakeStackDefinition>();
                _config = ScriptableObject.CreateInstance<DemoConfig>();

                Arena = new FakeArena(Log);
                Session = new FakeSession(Log);
                Stack = new FakeNetworkStack(Definition, Session, Directory);

                var stacks = new FakeStackFlow(Log, Slot) { Stack = Stack };
                Flow = new MatchFlow(Slot, stacks, Arena, _config, new[] { Box, Yard });
                Flow.Start();

                if (attachStack) Slot.Attach(Stack);
            }

            public void Dispose()
            {
                Flow.Dispose();
                Slot.Dispose();
                Directory.Dispose();

                UnityEngine.Object.DestroyImmediate(Box);
                UnityEngine.Object.DestroyImmediate(Yard);
                UnityEngine.Object.DestroyImmediate(Definition);
                UnityEngine.Object.DestroyImmediate(_config);
            }
        }

        [Test]
        public void PhaseFollowsTheStackSlot()
        {
            using var rig = new Rig(attachStack: false);
            Assert.AreEqual(AppPhase.SelectingStack, rig.Flow.Phase.CurrentValue);

            rig.Slot.Attach(rig.Stack);
            Assert.AreEqual(AppPhase.Lobby, rig.Flow.Phase.CurrentValue);
        }

        /// NGO спавнит игрока прямо внутри StartHost — без пола капсула улетает вниз. Мир уходит
        /// в сессию параметром, уровень — в метаданные (S3, И-2).
        [Test]
        public void ArenaLoadsBeforeSessionAndWorldIsPassed()
        {
            using var rig = new Rig();

            rig.Flow.HostAsync("Коля", rig.Box, CancellationToken.None).Forget();

            CollectionAssert.AreEqual(new[] { "arena", "host" }, rig.Log);
            Assert.AreSame(rig.Arena.World, rig.Session.World);
            Assert.AreEqual(rig.Box.ArenaId, rig.Session.Settings.Metadata[ArenaDefinition.METADATA_KEY]);
        }

        [Test]
        public void HostingSessionMeansMatch()
        {
            using var rig = new Rig();

            rig.Flow.HostAsync("Коля", rig.Box, CancellationToken.None).Forget();

            Assert.AreEqual(AppPhase.InMatch, rig.Flow.Phase.CurrentValue);
        }

        /// Пункт 8 чек-листа: закрытый хост не оставляет клиента в пустой арене.
        [Test]
        public void FailureIsReportedAndReturnsToLobby()
        {
            using var rig = new Rig();
            var failures = new List<string>();
            rig.Flow.Failures.Subscribe(failures.Add);
            rig.Flow.HostAsync("Коля", rig.Box, CancellationToken.None).Forget();

            rig.Session.Fail("Хост недоступен или отключился");

            CollectionAssert.AreEqual(new[] { "Хост недоступен или отключился" }, failures);
            CollectionAssert.AreEqual(new[] { "arena", "host", "leave", "unload" }, rig.Log);
            Assert.AreEqual(AppPhase.Lobby, rig.Flow.Phase.CurrentValue);
        }

        [Test]
        public void JoinGoesToArenaOfTheHost()
        {
            using var rig = new Rig();
            var entry = new HostEntry("Коля", 1, 4, "192.168.0.5:7777",
                new Dictionary<string, string> { [ArenaDefinition.METADATA_KEY] = "yard" });

            rig.Flow.JoinAsync(entry, rig.Box, CancellationToken.None).Forget();

            Assert.AreSame(rig.Yard, rig.Arena.Loaded);
            Assert.AreSame(entry, rig.Session.JoinedEntry);
        }

        /// S10: запись из ручного ввода уровня не несёт — идём на отмеченный у себя.
        [Test]
        public void ManualEntryUsesFallbackArena()
        {
            using var rig = new Rig();

            rig.Flow.JoinAsync(new HostEntry("вручную", 0, 0, "10.0.0.2:7777", null), rig.Yard, CancellationToken.None).Forget();

            Assert.AreSame(rig.Yard, rig.Arena.Loaded);
        }

        [Test]
        public void UnknownArenaIsRefusedWithMessage()
        {
            using var rig = new Rig();
            var failures = new List<string>();
            rig.Flow.Failures.Subscribe(failures.Add);
            var entry = new HostEntry("Коля", 1, 4, "x", new Dictionary<string, string> { [ArenaDefinition.METADATA_KEY] = "ghost" });

            rig.Flow.JoinAsync(entry, rig.Box, CancellationToken.None).Forget();

            CollectionAssert.AreEqual(new[] { "Хост играет на уровне, которого нет в этой сборке" }, failures);
            CollectionAssert.IsEmpty(rig.Log);
        }

        [Test]
        public void LeaveClosesSessionThenArena()
        {
            using var rig = new Rig();
            rig.Flow.HostAsync("Коля", rig.Box, CancellationToken.None).Forget();

            rig.Flow.LeaveAsync().Forget();

            CollectionAssert.AreEqual(new[] { "arena", "host", "leave", "unload" }, rig.Log);
            Assert.AreEqual(AppPhase.Lobby, rig.Flow.Phase.CurrentValue);
        }

        /// Рвём снизу вверх: сессия, арена, сцена стека.
        [Test]
        public void BackToStacksTearsDownBottomUp()
        {
            using var rig = new Rig();
            rig.Flow.HostAsync("Коля", rig.Box, CancellationToken.None).Forget();

            rig.Flow.BackToStacksAsync().Forget();

            CollectionAssert.AreEqual(new[] { "arena", "host", "leave", "unload", "stack-unload" }, rig.Log);
            Assert.AreEqual(AppPhase.SelectingStack, rig.Flow.Phase.CurrentValue);
        }

        /// И-12 на уровне сценария: отменённая загрузка всё равно прибирает арену.
        [Test]
        public void CancelledStartUnloadsArena()
        {
            using var rig = new Rig();
            rig.Arena.CancelNextLoad = true;

            rig.Flow.HostAsync("Коля", rig.Box, CancellationToken.None).Forget();

            CollectionAssert.AreEqual(new[] { "arena", "unload" }, rig.Log);
            Assert.AreEqual(AppPhase.Lobby, rig.Flow.Phase.CurrentValue);
        }
    }
}
