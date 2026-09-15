using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace Game.Net
{
    /// Контракт (спека § 5.3), выполняется общим SessionStatePublisher:
    /// 1. реализация не бросает — любой отказ становится Failed с причиной для игрока (И-11);
    /// 2. собственный выход никогда не даёт Failed (А-2, И-10);
    /// 3. смена состояния доходит до подписчиков после выхода стека из своего коллбэка (А-3).
    ///
    /// Мир приходит параметром: точки спавна и фабрика нужны стеку уже внутри старта — NGO
    /// спрашивает место игрока в подтверждении подключения (И-2).
    public interface INetSession
    {
        public ReadOnlyReactiveProperty<SessionState> State { get; }
        public PlayerId LocalPlayer { get; }
        public bool IsJudge { get; }
        public IReadOnlyCollection<PlayerId> Players { get; }
        public Observable<PlayerId> PlayerJoined { get; }
        public Observable<PlayerId> PlayerLeft { get; }

        public UniTask StartHostAsync(SessionSettings settings, INetWorld world, CancellationToken token);
        public UniTask JoinAsync(HostEntry entry, INetWorld world, CancellationToken token);
        public UniTask LeaveAsync();
    }
}
