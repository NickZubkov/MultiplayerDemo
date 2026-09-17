using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using R3;
using UnityEngine;

namespace Game.Net.Fusion
{
    /// Список сессий приходит из Photon Cloud — ни маяка, ни UDP, ни таймеров, ни TTL. Ровно
    /// ради этого различия и заведён контракт каталога: у NGO поток собирается из UDP-пакетов
    /// вручную, у Mirror его собирает штатный поиск, здесь его целиком поставляет облако, а нам
    /// остаётся перевести SessionInfo в HostEntry.
    ///
    /// Метаданные тоже едут по-своему: у NGO в маяке, у Mirror в ответе поиска, здесь — в
    /// свойствах сессии, которые облако раздаёт всем, кто смотрит список. Рассказывать о себе
    /// каталогу при этом нечем: свойства объявляются при создании комнаты и живут в
    /// StartGameArgs — этим занимается сессия.
    public sealed class FusionDirectory : IHostDirectory
    {
        private const string NO_LOBBY = "Каталог Fusion: в лобби Photon войти не удалось — ";

        /// После отказа ждём секунду: повод штатный — нет сети, спит Wi-Fi, молчит облако, —
        /// и долбиться в него каждый кадр незачем.
        private static readonly TimeSpan RETRY_INTERVAL = TimeSpan.FromSeconds(1);

        private readonly FusionRunnerFactory _runners;
        private readonly HostListPublisher _publisher = new();

        /// Вход в лобби — единственная операция стека, которая переживает свой scope: ответ от
        /// Photon приходит через сеть и может застать разобранный контейнер.
        private readonly CancellationTokenSource _life = new();

        /// Копия токена, а не обращение к источнику по месту: продолжение цикла просыпается
        /// уже после Dispose источника — выход из Play Mode приходится ровно на ожидание
        /// ответа Photon, — а закрытый CancellationTokenSource на запрос Token бросает.
        private readonly CancellationToken _token;

        private bool _browsing;
        private bool _running;
        private bool _complained;

        public Observable<IReadOnlyList<HostEntry>> Hosts => _publisher.Hosts;

        public FusionDirectory(FusionRunnerFactory runners)
        {
            _runners = runners;
            _token = _life.Token;
        }

        /// Отменяем здесь, а не в StopBrowsing: раннер у каталога и у сессии один и тот же, а
        /// отменённый вход в лобби Fusion доводит до Shutdown раннера — прекращение просмотра
        /// списка посреди матча убило бы сам матч. В Dispose уходит всё разом, и это
        /// единственный момент, когда такая отмена безопасна.
        ///
        /// Порядок VContainer здесь на нашей стороне: контейнер разбирает созданное стеком, то
        /// есть каталог гасится раньше фабрики раннера — и к моменту, когда та дёрнет Shutdown,
        /// продолжение входа в лобби уже никого не потревожит.
        public void Dispose()
        {
            StopBrowsing();
            _life.Cancel();
            _life.Dispose();
            _publisher.Dispose();
        }

        /// Цикл поиска один на весь каталог: второй запуск, пока первый ещё висит на ответе
        /// облака, дал бы два входа в лобби на одном раннере.
        public void StartBrowsing()
        {
            _browsing = true;

            if (_running) return;

            BrowseAsync().Forget();
        }

        public void StopBrowsing() => _browsing = false;

        /// Ручной ввод у Fusion — имя сессии, а не адрес: комнату в облаке ищут по имени.
        /// Счётчики и метаданные взять неоткуда — запись этой сессии могла и не дойти до
        /// списка. Уровень клиент возьмёт из отмеченного в лобби.
        public bool TryParseManual(string text, out HostEntry entry)
        {
            entry = null;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var name = text.Trim();
            entry = new HostEntry(name, 0, 0, name, null);
            return true;
        }

        /// Зовёт мост коллбэков. Список приходит целиком, поэтому и публикуем его целиком: ни
        /// склейки, ни истечения по TTL здесь нет — что прислало облако, то и есть правда.
        /// Счётчик игроков в нём живой (И-7): облако присылает список заново на каждое
        /// изменение комнаты.
        public void OnSessionListUpdated(List<SessionInfo> sessions)
        {
            var entries = new List<HostEntry>(sessions.Count);

            foreach (var session in sessions)
            {
                if (!session.IsOpen || !session.IsVisible) continue;

                entries.Add(new HostEntry(session.Name, session.PlayerCount, session.MaxPlayers, session.Name,
                    MetadataOf(session)));
            }

            _publisher.Publish(entries);
        }

        /// Редкий тик вместо покадрового насоса NGO и Mirror: список присылает облако само,
        /// нам остаётся следить, что мы в лобби. Раннер у Fusion одноразовый — после матча
        /// фабрика делает новый, и войти в лобби надо заново, иначе список замрёт на том, что
        /// было до матча. Отказ облака поиск не гасит (И-5): жалоба уходит один раз, попытка
        /// повторяется на следующем тике.
        private async UniTaskVoid BrowseAsync()
        {
            _running = true;

            while (_browsing)
            {
                if (NeedsLobby(_runners.Ensure())) await JoinLobbyAsync();

                if (!_browsing) break;

                var canceled = await UniTask.Delay(RETRY_INTERVAL, cancellationToken: _token)
                    .SuppressCancellationThrow();

                if (canceled) break;
            }

            _running = false;
        }

        /// Второй вход в то же лобби Photon встречает отказом «Client still connected», а вход
        /// поверх старта матча кладёт обе операции: раннер один и на лобби, и на сессию.
        private static bool NeedsLobby(NetworkRunner runner) =>
            !runner.LobbyInfo.IsValid && !runner.IsStarting && !runner.IsInSession;

        private async UniTask JoinLobbyAsync()
        {
            /// Настройки те же, что у старта сессии, и подложить сводку регионов надо в оба
            /// места: в лобби каталог входит первым, и полный пинг случается там.
            var result = await _runners.Ensure()
                .JoinSessionLobby(SessionLobby.Shared,
                    customAppSettings: PhotonRegionSummary.WithStoredRegion(),
                    cancellationToken: _token);

            if (!_browsing || result.Ok) return;

            Fail(result);

            /// Пустое лобби обязано быть подписано причиной: молчаливо пустой список на
            /// демонстрации читается как поломка, а строка в консоли — как объяснение.
            _publisher.Publish(Array.Empty<HostEntry>());
        }

        /// Жалуемся один раз — иначе потерянная сеть залила бы консоль строкой в секунду.
        private void Fail(StartGameResult result)
        {
            if (_complained) return;

            _complained = true;
            var reason = string.IsNullOrEmpty(result.ErrorMessage) ? result.ShutdownReason.ToString() : result.ErrorMessage;
            Debug.LogWarning($"{NO_LOBBY}{reason}. Поиск продолжится сам.");
        }

        /// Свойства комнаты у Photon бывают трёх типов; сеть возит словарь строк и не знает,
        /// что в нём лежит уровень, — чужой тип просто не наш.
        private static IReadOnlyDictionary<string, string> MetadataOf(SessionInfo session)
        {
            var properties = session.Properties;
            if (properties == null || properties.Count == 0) return null;

            var metadata = new Dictionary<string, string>(properties.Count);

            foreach (var pair in properties)
            {
                if (pair.Value != null && pair.Value.IsString) metadata[pair.Key] = (string)pair.Value;
            }

            return metadata;
        }
    }
}
