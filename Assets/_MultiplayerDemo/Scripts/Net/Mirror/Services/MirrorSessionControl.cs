using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Mirror;
using R3;

namespace Game.Net.Mirror
{
    /// Порт по умолчанию — 7778, он выставлен транспорту в сцене Net_Mirror; здесь
    /// константа нужна только подписи ручного ввода в Stack_Mirror. Реальный порт
    /// подключения приезжает в маяке хоста, а при ручном вводе — из строки адреса.
    ///
    /// О подключении и разрыве Mirror сообщает переопределяемыми методами менеджера,
    /// а не событиями: NetworkClient.OnConnectedEvent он присваивает себе сам
    /// (NetworkManager.RegisterClientMessages), и чужая подписка там не выживет.
    /// Поэтому события приходят сюда от MirrorNetworkManager вызовами Report*.
    public sealed class MirrorSessionControl : ISessionControl, IDisposable
    {
        public const ushort Port = 7778;

        private const string LostReason = "Хост недоступен или отключился";

        private readonly NetworkManager _manager;
        private readonly ReactiveProperty<SessionState> _state = new(new SessionState(SessionPhase.Idle));

        private bool _reportingLoss;

        public ReadOnlyReactiveProperty<SessionState> State => _state;

        public MirrorSessionControl(NetworkManager manager)
        {
            _manager = manager;
        }

        public void Dispose() => _state.Dispose();

        /// Уровень здесь не нужен: его отдаст MirrorHostBrowser в ответе на поиск.
        public UniTask StartHostAsync(string playerName, string arenaId, CancellationToken token)
        {
            _manager.StartHost();

            /// StartHost у Mirror ничего не возвращает и об отказе сообщает только в лог,
            /// поэтому успех проверяем по факту поднятого сервера.
            _state.Value = NetworkServer.active
                ? new SessionState(SessionPhase.Hosting)
                : new SessionState(SessionPhase.Failed, "Не удалось поднять хост");

            return UniTask.CompletedTask;
        }

        public async UniTask JoinAsync(HostEntry entry, CancellationToken token)
        {
            var parts = entry.JoinToken.Split(':');

            if (parts.Length != 2 || !ushort.TryParse(parts[1], out var port))
            {
                _state.Value = new SessionState(SessionPhase.Failed, $"Непонятный адрес: {entry.JoinToken}");
                return;
            }

            /// У Mirror адрес и порт живут врозь: имя хоста — на менеджере, порт — на
            /// транспорте. Разберённый токен раскладываем по обоим, иначе введённый
            /// вручную порт молча потеряется и клиент постучится не туда.
            _manager.networkAddress = parts[0];

            if (_manager.transport is PortTransport transport)
            {
                transport.Port = port;
            }

            _state.Value = new SessionState(SessionPhase.Connecting);
            _manager.StartClient();

            /// Таймаут живёт здесь, а не в презентере: молчаливое зависание в лобби —
            /// худший вид ошибки в сетевом приложении.
            try
            {
                await _state.Where(s => s.Phase != SessionPhase.Connecting)
                            .Timeout(TimeSpan.FromSeconds(5))
                            .FirstAsync(token);
            }
            catch (TimeoutException)
            {
                _state.Value = new SessionState(SessionPhase.Failed, "Хост не ответил за 5 секунд");
            }
        }

        /// Фазу гасим до остановки, а не после: Mirror зовёт OnClientDisconnect и на
        /// собственный StopClient тоже, а по фазе Idle мост отличает свой выход от
        /// чужого разрыва. Иначе выход в лобби показывал бы игроку «хост отключился».
        public UniTask LeaveAsync()
        {
            _state.Value = new SessionState(SessionPhase.Idle);

            /// Пришли из собственного OnClientDisconnect Mirror — останавливать нечего:
            /// он уже посреди своей остановки и сам доведёт её до конца, а повторный вход
            /// в StopClient пошёл бы по полуразобранному состоянию.
            if (_reportingLoss) return UniTask.CompletedTask;

            if (NetworkServer.active) _manager.StopHost();
            else if (NetworkClient.active) _manager.StopClient();

            return UniTask.CompletedTask;
        }

        /// На хосте локальный клиент подключается к себе же внутри StartHost — фазу
        /// матча в этом случае объявляет StartHostAsync, и перебивать её незачем.
        public void ReportConnected()
        {
            if (NetworkServer.active) return;
            _state.Value = new SessionState(SessionPhase.Connected);
        }

        /// Два случая, которые разрывом сессии не являются, хотя Mirror зовёт на них то же самое.
        ///
        /// На хосте локальный клиент отваливается только внутри нашего же StopHost: чужие уходят
        /// через OnServerDisconnect, которого мы не слушаем. Отличать обязательно — сообщение
        /// об отказе доводит презентер до LeaveAsync, тот зовёт StopHost повторно, уже посреди
        /// первой остановки, где NetworkServer.localConnection снят, и Mirror падает с
        /// NullReferenceException в DestroyPlayerForConnection.
        ///
        /// Фаза Idle означает, что уход уже объявлен нами в LeaveAsync, — тоже свой выход.
        public void ReportDisconnected(string reason)
        {
            if (NetworkServer.active) return;
            if (_state.Value.Phase == SessionPhase.Idle) return;

            /// Присвоение разбирается подписчиками синхронно, прямо здесь: презентер
            /// покажет причину и позовёт LeaveAsync до возврата из этой строки.
            _reportingLoss = true;

            try
            {
                _state.Value = new SessionState(SessionPhase.Failed,
                    string.IsNullOrEmpty(reason) ? LostReason : reason);
            }
            finally
            {
                _reportingLoss = false;
            }
        }
    }
}
