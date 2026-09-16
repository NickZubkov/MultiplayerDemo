using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirror;
using R3;

namespace Game.Net.Mirror
{
    /// Сессия Mirror: хост — это сервер и клиент в одном процессе, судья — он же. Правила
    /// контракта (не бросать, свой выход не Failed, отчёт следующим кадром) исполняет общий
    /// публикатор — здесь только факты стека.
    ///
    /// О подключении и разрыве Mirror сообщает переопределяемыми методами менеджера, а не
    /// событиями: NetworkClient.OnConnectedEvent он присваивает себе сам в RegisterClientMessages,
    /// и чужая подписка там не выживет. Поэтому события приходят сюда от MirrorNetworkManager
    /// вызовами Report*.
    ///
    /// Заплаты на реентерабельность (прежний флаг _reportingLoss) больше нет: публикатор доносит
    /// смену состояния следующим кадром, и презентер не успевает позвать LeaveAsync изнутри
    /// коллбэка Mirror (А-3).
    public sealed class MirrorSession : INetSession, IDisposable
    {
        private const string HOST_FAILED = "Не удалось поднять хост";
        private const string CONNECT_FAILED = "Не удалось начать подключение";
        private const string NO_ANSWER = "Хост не ответил за 5 секунд";
        private const string DISCONNECTED = "Хост недоступен или отключился";
        private const string BAD_ADDRESS = "Непонятный адрес: ";

        /// Молчаливое зависание в лобби — худший вид ошибки в сетевом приложении, поэтому
        /// таймаут живёт здесь, а не в презентере.
        private static readonly TimeSpan CONNECT_TIMEOUT = TimeSpan.FromSeconds(5);

        private readonly NetworkManager _manager;
        private readonly MirrorDirectory _directory;
        private readonly MirrorSpawner _spawner;
        private readonly MirrorPlayers _players;
        private readonly SessionStatePublisher _publisher;

        public ReadOnlyReactiveProperty<SessionState> State => _publisher.State;
        public PlayerId LocalPlayer => _players.Local;
        public bool IsJudge => NetworkServer.active;
        public IReadOnlyCollection<PlayerId> Players => _players.All;
        public Observable<PlayerId> PlayerJoined => _players.Joined;
        public Observable<PlayerId> PlayerLeft => _players.Left;

        public MirrorSession(NetworkManager manager, MirrorDirectory directory, MirrorSpawner spawner,
            MirrorPlayers players, FrameProvider frames)
        {
            _manager = manager;
            _directory = directory;
            _spawner = spawner;
            _players = players;
            _publisher = new SessionStatePublisher(frames);
        }

        public void Dispose() => _publisher.Dispose();

        /// Мир отдаём спавнеру до старта: аватар хоста Mirror создаёт сразу после того, как его
        /// же локальный клиент объявит себя готовым, и место игрока нужно спавнеру уже там.
        public UniTask StartHostAsync(SessionSettings settings, INetWorld world, CancellationToken token)
        {
            _publisher.Begin(SessionPhase.Hosting);
            _spawner.Use(world);
            return _publisher.GuardAsync(() => HostAsync(settings));
        }

        public UniTask JoinAsync(HostEntry entry, INetWorld world, CancellationToken token)
        {
            _publisher.Begin(SessionPhase.Connecting);
            _spawner.Use(world);
            return _publisher.GuardAsync(() => ConnectAsync(entry, token));
        }

        /// Публикатор закрывается раньше остановки: разрывы, которые Mirror разошлёт следом,
        /// не должны стать ложным «хост отключился» на выходе в лобби (И-10).
        public UniTask LeaveAsync()
        {
            _directory.StopAdvertising();
            _publisher.End();

            if (NetworkServer.active) _manager.StopHost();
            else if (NetworkClient.active) _manager.StopClient();

            _spawner.Forget();
            _players.Reset();
            return UniTask.CompletedTask;
        }

        /// Мост: локальный клиент подключился. На хосте он подключается к себе же внутри
        /// StartHost — фазу матча там объявляет StartHostAsync, и перебивать её незачем.
        public void ReportConnected()
        {
            if (NetworkServer.active) return;

            _publisher.Report(new SessionState(SessionPhase.Connected));
        }

        /// Мост: клиента разорвало. Свой выход публикатор уже закрыл, и этот отчёт он отбросит.
        public void ReportLost(string reason)
        {
            if (NetworkServer.active) return;

            _publisher.Report(new SessionState(SessionPhase.Failed,
                string.IsNullOrEmpty(reason) ? DISCONNECTED : reason));
        }

        private UniTask HostAsync(SessionSettings settings)
        {
            _manager.StartHost();

            /// StartHost у Mirror ничего не возвращает и об отказе сообщает только в лог,
            /// поэтому успех проверяем по факту поднятого сервера.
            if (!NetworkServer.active) throw new InvalidOperationException(HOST_FAILED);

            /// Порядок иной, чем у NGO: аватар хоста появится позже сущностей мира — локальный
            /// клиент просит его отдельным сообщением, и очередь хоста разберёт её следующим кадром.
            _spawner.PopulateWorld();
            _directory.Advertise(settings);
            return UniTask.CompletedTask;
        }

        private async UniTask ConnectAsync(HostEntry entry, CancellationToken token)
        {
            var fallback = MirrorAddress.PortOf(_manager.transport);

            if (!MirrorAddress.TryParse(entry.JoinToken, fallback, out var address, out var port))
            {
                throw new InvalidOperationException(BAD_ADDRESS + entry.JoinToken);
            }

            /// У Mirror адрес и порт живут врозь: имя хоста — на менеджере, порт — на транспорте.
            /// Разобранный токен раскладываем по обоим, иначе введённый вручную порт молча
            /// потеряется и клиент постучится не туда.
            _manager.networkAddress = address;

            if (_manager.transport is PortTransport transport) transport.Port = port;

            _manager.StartClient();

            if (!NetworkClient.active) throw new InvalidOperationException(CONNECT_FAILED);

            /// Первое значение при подписке — то, что в свойстве уже лежит: публикатор доносит
            /// наш Connecting только следующим кадром. Его пропускаем и ждём терминальную фазу,
            /// а не «любую, кроме Connecting», — иначе прошлое состояние сошло бы за ответ хоста.
            try
            {
                await _publisher.State
                                .Skip(1)
                                .Where(state => state.Phase is SessionPhase.Connected or SessionPhase.Failed)
                                .Timeout(CONNECT_TIMEOUT)
                                .FirstAsync(token);
            }
            catch (TimeoutException)
            {
                throw new InvalidOperationException(NO_ANSWER);
            }
        }
    }
}
