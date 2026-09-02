using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace Game.Core
{
    /// Состояние — свойство-поток: подписчик получает текущее значение сразу,
    /// а не ждёт следующего события. Токен привязан к жизни scope стека.
    ///
    /// Асинхронные сигнатуры не украшение: у Fusion StartGame асинхронен по своей
    /// природе, и синхронная обёртка давала бы async void с непойманным исключением
    /// при выходе из лобби посреди подключения.
    public interface ISessionControl
    {
        public ReadOnlyReactiveProperty<SessionState> State { get; }

        public UniTask StartHostAsync(string playerName, CancellationToken token);
        public UniTask JoinAsync(HostEntry entry, CancellationToken token);
        public UniTask LeaveAsync();
    }
}
