using System;
using System.Collections.Generic;
using Game.Core;
using Game.Net;
using Game.UI;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace Game.Tests
{
    /// Лобби после разделения обязанностей (А-6): только экран — список хостов, выбор
    /// уровня, кнопки. Хост, подключение и отказы проверяются в MatchFlowTests.
    public sealed class LobbyPresenterTests
    {
        private sealed class FakeLobbyView : ILobbyView
        {
            private readonly Subject<string> _host = new();
            private readonly Subject<HostEntry> _join = new();
            private readonly Subject<string> _manual = new();
            private readonly Subject<ArenaDefinition> _arena = new();
            private readonly Subject<Unit> _back = new();

            public IReadOnlyList<HostRow> Rows = Array.Empty<HostRow>();
            public ArenaDefinition Marked;
            public string ManualHint;
            public string HostLabel;
            public bool JoinVisible = true;
            public bool Interactable = true;

            public Observable<string> HostRequested => _host;
            public Observable<HostEntry> JoinRequested => _join;
            public Observable<string> ManualJoinRequested => _manual;
            public Observable<ArenaDefinition> ArenaChosen => _arena;
            public Observable<Unit> BackToStacksRequested => _back;

            public void Show()
            {
            }

            public void Hide()
            {
            }

            public void ShowHosts(IReadOnlyList<HostRow> hosts) => Rows = hosts;

            public void ShowArenas(IReadOnlyList<ArenaDefinition> arenas)
            {
            }

            public void MarkArena(ArenaDefinition arena) => Marked = arena;

            public void SetEmptyHint(string text)
            {
            }

            public void SetManualHint(string text) => ManualHint = text;

            public void SetHostLabel(string text) => HostLabel = text;

            public void SetJoinVisible(bool visible) => JoinVisible = visible;

            public void SetInteractable(bool interactable) => Interactable = interactable;

            public void ClickHost(string name) => _host.OnNext(name);

            public void ClickJoin(HostEntry entry) => _join.OnNext(entry);

            public void ClickManual(string text) => _manual.OnNext(text);

            public void ChooseArena(ArenaDefinition arena) => _arena.OnNext(arena);
        }

        private sealed class Rig : IDisposable
        {
            public readonly FakeLobbyView View = new();
            public readonly NetworkSlot Slot = new();
            public readonly FakeDirectory Directory = new();
            public readonly FakeMatchFlow Match = new();
            public readonly FakeHud Hud = new();
            public readonly FakeStackDefinition Definition;
            public readonly LobbyPresenter Presenter;
            public readonly ArenaDefinition Box;
            public readonly ArenaDefinition Yard;

            public Rig(bool canJoin = true, string hostLabel = "Поднять хост")
            {
                Box = ScriptableObject.CreateInstance<ArenaDefinition>();
                Yard = ScriptableObject.CreateInstance<ArenaDefinition>();

                /// Поля описаний приватные и сериализованные: ключи JSON — имена полей,
                /// с подчёркиванием. Название у второго уровня своё, иначе подпись строки
                /// хоста нечем было бы отличить от умолчания.
                JsonUtility.FromJsonOverwrite(
                    "{\"_displayName\":\"Двор\",\"_arenaId\":\"yard\",\"_sceneName\":\"Arena_Yard\"}", Yard);

                Definition = ScriptableObject.CreateInstance<FakeStackDefinition>();
                JsonUtility.FromJsonOverwrite(
                    $"{{\"_canJoin\":{(canJoin ? "true" : "false")},\"_hostButtonLabel\":\"{hostLabel}\"}}",
                    Definition);

                Slot.Attach(new FakeNetworkStack(Definition, null, Directory));

                Presenter = new LobbyPresenter(View, Slot, Match, Hud, new[] { Box, Yard });
                Presenter.Start();
            }

            public void Dispose()
            {
                Presenter.Dispose();
                Slot.Dispose();
                Directory.Dispose();

                UnityEngine.Object.DestroyImmediate(Box);
                UnityEngine.Object.DestroyImmediate(Yard);
                UnityEngine.Object.DestroyImmediate(Definition);
            }
        }

        [Test]
        public void WantedOnlyInLobbyAndStarting()
        {
            using var rig = new Rig();

            Assert.IsTrue(rig.Presenter.Wanted.CurrentValue);

            rig.Match.Current.Value = AppPhase.Starting;
            Assert.IsTrue(rig.Presenter.Wanted.CurrentValue);
            Assert.IsFalse(rig.View.Interactable);

            rig.Match.Current.Value = AppPhase.InMatch;
            Assert.IsFalse(rig.Presenter.Wanted.CurrentValue);
        }

        [Test]
        public void HostUsesChosenArena()
        {
            using var rig = new Rig();

            rig.View.ChooseArena(rig.Yard);
            rig.View.ClickHost("Коля");

            Assert.AreSame(rig.Yard, rig.View.Marked);
            Assert.AreSame(rig.Yard, rig.Match.HostedArena);
            Assert.AreEqual("Коля", rig.Match.HostedBy);
        }

        /// S10: для записи без уровня сценарий получает отмеченный в лобби.
        [Test]
        public void JoinPassesChosenArenaAsFallback()
        {
            using var rig = new Rig();
            var entry = new HostEntry("Коля", 1, 4, "192.168.0.5:7777", null);

            rig.View.ClickJoin(entry);

            Assert.AreSame(entry, rig.Match.JoinedEntry);
            Assert.AreSame(rig.Box, rig.Match.JoinFallback);
        }

        /// Лобби читает данные описания стека и не спрашивает, что это за стек.
        [Test]
        public void StackDataReachesTheView()
        {
            using var rig = new Rig(canJoin: false, hostLabel: "Играть");

            rig.Presenter.Show();

            Assert.IsFalse(rig.View.JoinVisible);
            Assert.AreEqual("Играть", rig.View.HostLabel);
            Assert.AreEqual(rig.Definition.ManualEntryHint, rig.View.ManualHint);
        }

        [Test]
        public void ManualTextIsParsedByTheStack()
        {
            using var rig = new Rig();
            var parsed = new HostEntry("вручную", 0, 0, "10.0.0.2:7777", null);
            rig.Directory.Parsed = parsed;

            rig.View.ClickManual("10.0.0.2:7777");

            Assert.AreSame(parsed, rig.Match.JoinedEntry);
        }

        [Test]
        public void UnparsedManualTextIsExplained()
        {
            using var rig = new Rig();

            rig.View.ClickManual("куда-то");

            Assert.IsNull(rig.Match.JoinedEntry);
            StringAssert.Contains("куда-то", rig.Hud.Last);
        }

        [Test]
        public void HostRowsShowArenaName()
        {
            using var rig = new Rig();
            rig.Presenter.Show();

            rig.Directory.HostList.Value = new[]
            {
                new HostEntry("Коля", 2, 4, "t", new Dictionary<string, string> { [ArenaDefinition.METADATA_KEY] = "yard" }),
            };

            StringAssert.Contains(rig.Yard.DisplayName, rig.View.Rows[0].Label);
            StringAssert.Contains("2/4", rig.View.Rows[0].Label);
        }

        /// Поиск хостов идёт, только пока лобби на экране.
        [Test]
        public void BrowsingOnlyWhileShown()
        {
            using var rig = new Rig();

            rig.Presenter.Show();
            Assert.IsTrue(rig.Directory.Browsing);

            rig.Presenter.Hide();
            Assert.IsFalse(rig.Directory.Browsing);
        }
    }
}
