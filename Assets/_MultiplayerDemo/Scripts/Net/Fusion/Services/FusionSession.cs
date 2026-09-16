using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using R3;
using UnityEngine;

namespace Game.Net.Fusion
{
    /// Сессия Fusion в Shared Mode: сервера нет, у каждого объекта свой владелец состояния.
    /// «Поднять хост» и «подключиться» — одна и та же операция StartGame, разница лишь в том,
    /// кто первым создал комнату и разрешено ли создавать её по имени. Судья — мастер-клиент
    /// комнаты (Ц7), то есть тот, кто её создал, а после его ухода — следующий.
    ///
    /// Правила контракта (не бросать, свой выход не Failed, отчёт следующим кадром) исполняет
    /// общий публикатор — здесь только факты стека.
    public sealed class FusionSession : INetSession, IDisposable
    {
        private const string NO_SESSION_NAME = "Не указано имя сессии";
        private const string START_FAILED = "Не удалось начать сессию";

        /// Сразу после StartGame статистики ещё нет — замер задержки ждёт реального трафика.
        private static readonly TimeSpan CONDITIONS_DELAY = TimeSpan.FromSeconds(5);

        private readonly FusionRunnerFactory _runners;
        private readonly FusionSpawner _spawner;
        private readonly SessionStatePublisher _publisher;
        private readonly PlayerView _players;
        private readonly Subject<PlayerId> _joined = new();
        private readonly Subject<PlayerId> _left = new();

        /// Идёт ли наш матч. На выходе Fusion гасит объекты и рассылает уход каждого игрока,
        /// и превращать это в PlayerLeft нельзя: логика механик полезла бы к сущностям,
        /// которых уже нет.
        private bool _inSession;

        public ReadOnlyReactiveProperty<SessionState> State => _publisher.State;
        public PlayerId LocalPlayer => Live == null ? PlayerId.NONE : FusionIds.Player(Live.LocalPlayer);
        public bool IsJudge => Live != null && Live.IsSharedModeMasterClient;
        public IReadOnlyCollection<PlayerId> Players => _players;
        public Observable<PlayerId> PlayerJoined => _joined;
        public Observable<PlayerId> PlayerLeft => _left;

        private NetworkRunner Live => _runners.Live;

        public FusionSession(FusionRunnerFactory runners, FusionSpawner spawner, FrameProvider frames)
        {
            _runners = runners;
            _spawner = spawner;
            _publisher = new SessionStatePublisher(frames);
            _players = new PlayerView(runners);
        }

        public void Dispose()
        {
            _publisher.Dispose();
            _joined.Dispose();
            _left.Dispose();
        }

        /// Мир отдаём спавнеру до StartGame: свой аватар создаётся из OnPlayerJoined, а тот
        /// приходит ещё внутри старта.
        public UniTask StartHostAsync(SessionSettings settings, INetWorld world, CancellationToken token)
        {
            _publisher.Begin(SessionPhase.Hosting);
            _spawner.Use(world);
            return _publisher.GuardAsync(() => HostAsync(settings, token));
        }

        public UniTask JoinAsync(HostEntry entry, INetWorld world, CancellationToken token)
        {
            _publisher.Begin(SessionPhase.Connecting);
            _spawner.Use(world);
            return _publisher.GuardAsync(() => ConnectAsync(entry, token));
        }

        /// Публикатор закрывается раньше остановки: OnShutdown Fusion зовёт изнутри Shutdown,
        /// и наш собственный выход не должен стать отказом (И-10).
        public async UniTask LeaveAsync()
        {
            _inSession = false;
            _publisher.End();
            await _runners.ShutdownAsync();
            _spawner.Forget();
        }

        /// Мост: раннер выключился не по нашей просьбе — разрыв связи, закрытая комната, отказ
        /// Photon. Свой выход публикатор уже закрыл, и этот отчёт он отбросит.
        public void ReportLost(ShutdownReason reason)
        {
            _inSession = false;
            _publisher.Report(new SessionState(SessionPhase.Failed, Describe(reason)));
        }

        /// Мост: в комнату вошёл игрок. Свой вход новостью не является — его видит тот, кто
        /// только что стартовал.
        public void ReportPlayerJoined(PlayerRef player)
        {
            if (!_inSession || Live == null || player == Live.LocalPlayer) return;

            _joined.OnNext(FusionIds.Player(player));
        }

        public void ReportPlayerLeft(PlayerRef player)
        {
            if (!_inSession || Live == null || player == Live.LocalPlayer) return;

            _left.OnNext(FusionIds.Player(player));
        }

        /// Имя сессии — это её адрес в облаке, поэтому к нику дописывается число: два игрока
        /// с одинаковым ником иначе попали бы в одну комнату молча.
        private async UniTask HostAsync(SessionSettings settings, CancellationToken token)
        {
            var name = $"{settings.Name}-{UnityEngine.Random.Range(1000, 9999)}";

            _inSession = true;
            var runner = await StartAsync(name, settings, true, token);

            /// Мир создаёт мастер-клиент, и на своей же комнате он им и стал. Аватар к этому
            /// моменту уже есть: OnPlayerJoined на себя приходит внутри StartGame.
            _spawner.PopulateWorld(runner);
        }

        /// Пустое имя отсекаем до StartGame: для Fusion это не «адреса нет», а «любая сессия» —
        /// он уходит в JoinRandom и возвращается с «No match found», а на живом AppId мог бы и
        /// подсесть к чужому матчу.
        private async UniTask ConnectAsync(HostEntry entry, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(entry.JoinToken)) throw new InvalidOperationException(NO_SESSION_NAME);

            _inSession = true;
            await StartAsync(entry.JoinToken, null, false, token);

            /// Успех у Fusion один на оба намерения — StartGame вернулся, значит мы в сессии, —
            /// но «подключаюсь» обязано стать «подключён». Коллбэка соединения здесь нет:
            /// сервера, который бы его прислал, в Shared Mode не существует.
            _publisher.Report(new SessionState(SessionPhase.Connected));
        }

        private async UniTask<NetworkRunner> StartAsync(string sessionName, SessionSettings settings, bool mayCreate,
            CancellationToken token)
        {
            var runner = _runners.Ensure();

            var result = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = sessionName,
                /// Число мест объявляет тот, кто создаёт комнату; подключающийся принимает то,
                /// что в ней уже стоит, — поэтому у него здесь null, а не ноль.
                PlayerCount = settings?.MaxPlayers,

                /// Единственный момент, когда метаданные можно объявить: Fusion кладёт эти
                /// свойства и в комнату, и в её видимую из лобби часть (BuildRoomArgs вместе
                /// с BuildSessionCustomPropertyHolders), а после создания остаётся только
                /// обновление уже существующих ключей.
                SessionProperties = PropertiesOf(settings),

                /// В Shared Mode все — клиенты, и по умолчанию клиенту создавать сессию
                /// запрещено. Поэтому флаг и разделяет «поднять» и «подключиться»: без него
                /// подключение к закрытой сессии молча создавало бы вторую с тем же именем.
                EnableClientSessionCreation = mayCreate,

                /// SceneManager и Scene оставлены пустыми намеренно (решение S12): сцены грузим
                /// сами, иначе стек утащит арену мимо ArenaLoader и ArenaScope останется без
                /// родителя. Fusion своего менеджера сцен не навязывает — его добавляют примеры
                /// (FusionBootstrap.cs:653), а не сам StartGame.
                StartGameCancellationToken = token,

                /// Копия глобальных настроек с подложенной сводкой прошлого замера регионов:
                /// без неё Photon пингует все регионы заново на каждом старте, и это те самые
                /// несколько секунд недорисованной сцены.
                CustomPhotonAppSettings = PhotonRegionSummary.WithStoredRegion(),
            }).AsUniTask();

            /// Флаг гасим здесь же: матч не начался, и коллбэки гаснущего раннера не должны
            /// дойти до логики механик уходом игрока. Причину дальше переведёт публикатор (И-11).
            if (!result.Ok)
            {
                _inSession = false;
                throw new InvalidOperationException(Describe(result));
            }

            ReportConditionsAsync(token).Forget();
            return runner;
        }

        /// В Shared Mode прямой связи между пирами нет: всё идёт через сервер выбранного
        /// региона, и видимая задержка складывается из двух концов до него. Регион при пустом
        /// FixedRegion выбирается пингом, то есть каждый раз заново и не всегда удачно —
        /// поэтому какой он и сколько до него, надо не гадать, а видеть.
        private async UniTaskVoid ReportConditionsAsync(CancellationToken token)
        {
            var canceled = await UniTask.Delay(CONDITIONS_DELAY, cancellationToken: token).SuppressCancellationThrow();

            if (canceled) return;

            var runner = Live;
            if (runner == null || !runner.SessionInfo.IsValid) return;

            /// Меряем только до облака, и другого измерения здесь быть не может: в Shared Mode
            /// пиры между собой не соединены. GetPlayerRtt по этой же причине отдаёт ноль —
            /// прямого канала до игрока не существует, и путь состояния до чужого экрана
            /// складывается из двух дорог до облака.
            var (average, last) = runner.GetRttToPhotonCloud();
            Debug.Log($"Fusion: регион {runner.SessionInfo.Region}, RTT до облака "
                + $"{average * 1000:F0} мс в среднем, {last * 1000:F0} мс последний");

            /// Тот же момент годится и чтобы запомнить регион: замер к нему заведомо закончен,
            /// а следующему запуску это сэкономит полный пинг всех регионов.
            PhotonRegionSummary.Remember();
        }

        /// null, а не пустой словарь: объявлять свойства — дело того, кто создаёт комнату,
        /// и у подключающегося их попросту нет.
        private static Dictionary<string, SessionProperty> PropertiesOf(SessionSettings settings)
        {
            var metadata = settings?.Metadata;
            if (metadata == null || metadata.Count == 0) return null;

            var properties = new Dictionary<string, SessionProperty>(metadata.Count);

            foreach (var pair in metadata)
            {
                properties[pair.Key] = pair.Value;
            }

            return properties;
        }

        private static string Describe(StartGameResult result)
        {
            if (result.ShutdownReason != ShutdownReason.Ok) return Describe(result.ShutdownReason);

            return string.IsNullOrEmpty(result.ErrorMessage) ? START_FAILED : result.ErrorMessage;
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

        /// Копию списка держать нельзя: игроки входят и выходят помимо наших коллбэков — при
        /// миграции мастера, при разрыве, — а ActivePlayers у Fusion живой и одинаковый на
        /// всех машинах.
        private sealed class PlayerView : IReadOnlyCollection<PlayerId>
        {
            private readonly FusionRunnerFactory _runners;

            public int Count
            {
                get
                {
                    var count = 0;

                    foreach (var _ in this)
                    {
                        count++;
                    }

                    return count;
                }
            }

            public PlayerView(FusionRunnerFactory runners)
            {
                _runners = runners;
            }

            public IEnumerator<PlayerId> GetEnumerator()
            {
                var runner = _runners.Live;
                if (runner == null) yield break;

                foreach (var player in runner.ActivePlayers)
                {
                    yield return FusionIds.Player(player);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
