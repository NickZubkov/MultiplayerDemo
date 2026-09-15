using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Net;
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

        /// Уровень приходит вместе с именем, хотя LAN-стекам он в этот момент не нужен:
        /// они расскажут о нём маяком уже после старта. У облака второго момента нет —
        /// Photon отдаёт в список сессий только те свойства, что объявлены при создании
        /// комнаты, и дописать их потом нельзя (RealtimeExtensions отвечает на такую
        /// попытку «Only existing custom properties can be updated»). Поэтому уровень
        /// обязан попасть в порт: иначе клиент Fusion не узнаёт, куда идёт хост.
        public UniTask StartHostAsync(string playerName, string arenaId, CancellationToken token);
        public UniTask JoinAsync(HostEntry entry, CancellationToken token);
        public UniTask LeaveAsync();
    }
}
