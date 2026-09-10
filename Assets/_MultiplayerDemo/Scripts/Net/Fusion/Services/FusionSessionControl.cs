using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using Game.Core;
using R3;

namespace Game.Net.Fusion
{
    /// Shared Mode: сервера нет, у каждого объекта свой владелец состояния. «Поднять хост»
    /// и «подключиться» — одна и та же операция StartGame, разница лишь в том, кто первым
    /// создал сессию и разрешено ли создавать её по имени.
    ///
    /// Здесь UniTask окупается заметнее всего: StartGame асинхронен по своей природе,
    /// и в версии без него это был бы async void — то есть непойманное исключение
    /// при выходе из лобби посреди подключения.
    public sealed class FusionSessionControl : ISessionControl, IDisposable
    {
        /// Схему свойств сессии задаёт тот, кто создаёт комнату, поэтому ключ живёт здесь,
        /// а FusionHostBrowser читает список по этой же константе.
        public const string ArenaKey = "arena";

        private readonly FusionRunnerFactory _runners;
        private readonly DemoConfig _config;
        private readonly ReactiveProperty<SessionState> _state = new(new SessionState(SessionPhase.Idle));

        public ReadOnlyReactiveProperty<SessionState> State => _state;

        public FusionSessionControl(FusionRunnerFactory runners, DemoConfig config)
        {
            _runners = runners;
            _config = config;
        }

        public void Dispose() => _state.Dispose();

        /// Имя сессии — это её адрес в облаке, поэтому к нику дописывается число:
        /// два игрока с одинаковым ником иначе попали бы в одну комнату молча.
        public UniTask StartHostAsync(string playerName, string arenaId, CancellationToken token) =>
            StartAsync($"{playerName}-{UnityEngine.Random.Range(1000, 9999)}", SessionPhase.Hosting, true, arenaId,
                token);

        /// Клиент комнату не создаёт, и свойства ему передавать нечем — уровень он,
        /// наоборот, вычитал из списка сессий ещё до того, как сюда попал.
        ///
        /// Пустое имя отсекаем до StartGame: для Fusion это не «адреса нет», а «любая
        /// сессия» — он уходит в JoinRandom и возвращается с «No match found», а на живом
        /// AppId мог бы и подсесть к чужому матчу. Приходит такое из поля ручного ввода,
        /// оставленного пустым; NGO и Mirror ровно так же отсекают неразбираемый адрес.
        public UniTask JoinAsync(HostEntry entry, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(entry.JoinToken))
            {
                _state.Value = new SessionState(SessionPhase.Failed, "Не указано имя сессии");
                return UniTask.CompletedTask;
            }

            return StartAsync(entry.JoinToken, SessionPhase.Connecting, false, null, token);
        }

        public async UniTask LeaveAsync()
        {
            /// Фазу гасим до остановки: Fusion зовёт OnShutdown изнутри Shutdown, и по
            /// фазе Idle мост отличает свой выход от разрыва связи.
            _state.Value = new SessionState(SessionPhase.Idle);
            await _runners.ShutdownAsync();
        }

        /// Вызывает мост коллбэков: раннер выключился не по нашей просьбе.
        public void ReportLost(ShutdownReason reason)
        {
            if (_state.Value.Phase == SessionPhase.Idle) return;
            _state.Value = new SessionState(SessionPhase.Failed, Describe(reason));
        }

        private async UniTask StartAsync(string sessionName, SessionPhase intent, bool mayCreate, string arenaId,
            CancellationToken token)
        {
            _state.Value = new SessionState(intent);

            var result = await _runners.Ensure().StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = sessionName,
                PlayerCount = _config.MaxPlayers,

                /// Единственный момент, когда уровень можно объявить: Fusion кладёт эти
                /// свойства и в комнату, и в её видимую из лобби часть (BuildRoomArgs
                /// вместе с BuildSessionCustomPropertyHolders), а после создания остаётся
                /// только обновление уже существующих ключей.
                SessionProperties = PropertiesFor(arenaId),

                /// В Shared Mode все — клиенты, и по умолчанию клиенту создавать сессию
                /// запрещено. Поэтому флаг и разделяет «поднять» и «подключиться»: без него
                /// подключение к закрытой сессии молча создавало бы вторую с тем же именем.
                EnableClientSessionCreation = mayCreate,

                /// SceneManager и Scene оставлены пустыми намеренно (решение S12):
                /// сцены грузим сами, иначе стек утащит арену мимо ArenaLoader и ArenaScope
                /// останется без родителя. Fusion своего менеджера сцен не навязывает —
                /// его добавляют примеры (FusionBootstrap.cs:653), а не сам StartGame.
                StartGameCancellationToken = token,
            }).AsUniTask();

            _state.Value = result.Ok
                ? new SessionState(Reached(intent))
                : new SessionState(SessionPhase.Failed, Describe(result));

            if (result.Ok) ReportConditionsAsync(token).Forget();
        }

        /// В Shared Mode прямой связи между пирами нет: всё идёт через сервер выбранного
        /// региона, и видимая задержка складывается из двух концов до него. Регион при пустом
        /// FixedRegion выбирается пингом, то есть каждый раз заново и не всегда удачно —
        /// поэтому какой он и сколько до него, надо не гадать, а видеть.
        ///
        /// Замер отложен: сразу после StartGame статистики ещё нет, нужен реальный трафик.
        /// Токен здесь тот же, что у StartGame, — он привязан к подписке презентера,
        /// и на разборе scope ожидание снимается вместе с ней.
        private async UniTaskVoid ReportConditionsAsync(CancellationToken token)
        {
            var canceled = await UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: token)
                                        .SuppressCancellationThrow();

            if (canceled) return;

            var runner = _runners.Live;
            if (runner == null || !runner.SessionInfo.IsValid) return;

            /// Меряем только до облака, и другого измерения здесь быть не может: в Shared Mode
            /// пиры между собой не соединены, весь обмен идёт через сервер региона. GetPlayerRtt
            /// по этой же причине отдаёт ноль — прямого канала до игрока не существует, и путь
            /// состояния до чужого экрана складывается из двух дорог до облака.
            var (average, last) = runner.GetRttToPhotonCloud();
            UnityEngine.Debug.Log($"Fusion: регион {runner.SessionInfo.Region}, RTT до облака "
                + $"{average * 1000:F0} мс в среднем, {last * 1000:F0} мс последний");
        }

        /// Успех у Fusion один на оба намерения — StartGame вернулся, значит мы в сессии, —
        /// но фаза после него разная. Хост остаётся Hosting, а «подключаюсь» обязано стать
        /// «подключён»: лобби прячется по Hosting или Connected, и клиент, застрявший
        /// в Connecting, так и смотрел бы на арену сквозь панель лобби. У NGO и Mirror
        /// в Connected переводит коллбэк соединения, здесь такого события нет — сервера,
        /// который бы его прислал, в Shared Mode не существует.
        private static SessionPhase Reached(SessionPhase intent) =>
            intent == SessionPhase.Connecting ? SessionPhase.Connected : intent;

        /// null, а не пустой словарь: объявлять свойства — дело того, кто создаёт комнату,
        /// и у клиента их попросту нет. Ровно с null это поле и работало до появления уровня.
        private static Dictionary<string, SessionProperty> PropertiesFor(string arenaId)
        {
            if (string.IsNullOrEmpty(arenaId)) return null;

            return new Dictionary<string, SessionProperty>
            {
                [ArenaKey] = arenaId,
            };
        }

        private static string Describe(StartGameResult result)
        {
            if (result.ShutdownReason != ShutdownReason.Ok) return Describe(result.ShutdownReason);
            return string.IsNullOrEmpty(result.ErrorMessage) ? "Не удалось начать сессию" : result.ErrorMessage;
        }

        /// Причину показываем игроку, поэтому переводим хотя бы то, что случается на демонстрации.
        private static string Describe(ShutdownReason reason)
        {
            switch (reason)
            {
                case ShutdownReason.GameNotFound: return "Сессии больше нет";
                case ShutdownReason.GameIsFull: return "В сессии нет мест";
                case ShutdownReason.GameClosed: return "Сессия закрыта";
                case ShutdownReason.MaxCcuReached: return "Исчерпан лимит подключений Photon";
                case ShutdownReason.PhotonCloudTimeout: return "Photon не ответил";
                case ShutdownReason.ConnectionTimeout: return "Соединение потеряно";
                case ShutdownReason.ConnectionRefused: return "Photon отказал в соединении";
                default: return $"Сессия закончилась: {reason}";
            }
        }
    }
}
