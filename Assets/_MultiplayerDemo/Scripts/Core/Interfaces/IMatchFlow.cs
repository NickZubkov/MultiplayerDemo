using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Net;
using R3;

namespace Game.Core
{
    /// Сценарий матча без единой ссылки на UI (спека § 8.1). Одна фаза приложения на всех:
    /// загружен ли стек и в каком состоянии сессия — сведено в неё.
    public interface IMatchFlow
    {
        public ReadOnlyReactiveProperty<AppPhase> Phase { get; }
        public Observable<string> Failures { get; }

        public UniTask HostAsync(string playerName, ArenaDefinition arena, CancellationToken token);

        /// fallback — отмеченный в лобби уровень для записи без уровня: её собрали из
        /// ручного ввода, маяка не было (S10).
        public UniTask JoinAsync(HostEntry entry, ArenaDefinition fallback, CancellationToken token);

        public UniTask LeaveAsync();
        public UniTask BackToStacksAsync();
    }
}
