using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Net;
using R3;

namespace Game.Tests
{
    public sealed class FakeMatchFlow : IMatchFlow
    {
        public readonly ReactiveProperty<AppPhase> Current = new(AppPhase.Lobby);
        public readonly Subject<string> FailureStream = new();

        public string HostedBy;
        public ArenaDefinition HostedArena;
        public HostEntry JoinedEntry;
        public ArenaDefinition JoinFallback;
        public bool Left;

        public ReadOnlyReactiveProperty<AppPhase> Phase => Current;
        public Observable<string> Failures => FailureStream;

        public UniTask HostAsync(string playerName, ArenaDefinition arena, CancellationToken token)
        {
            HostedBy = playerName;
            HostedArena = arena;
            return UniTask.CompletedTask;
        }

        public UniTask JoinAsync(HostEntry entry, ArenaDefinition fallback, CancellationToken token)
        {
            JoinedEntry = entry;
            JoinFallback = fallback;
            return UniTask.CompletedTask;
        }

        public UniTask LeaveAsync()
        {
            Left = true;
            return UniTask.CompletedTask;
        }

        public UniTask BackToStacksAsync() => UniTask.CompletedTask;
    }
}
