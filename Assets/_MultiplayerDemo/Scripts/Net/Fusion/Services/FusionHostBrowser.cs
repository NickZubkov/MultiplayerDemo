using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using Game.Core;
using R3;

namespace Game.Net.Fusion
{
    /// Список сессий приходит из Photon Cloud — ни маяка, ни UDP, ни таймеров, ни TTL.
    /// Ровно ради этого различия и заведён порт IHostBrowser: у NGO поток собирается из
    /// UDP-пакетов вручную, у Mirror его собирает штатный поиск, здесь его целиком
    /// поставляет облако, а нам остаётся перевести SessionInfo в HostEntry.
    ///
    /// Уровень тоже едет по-своему: у NGO в маяке, у Mirror в ответе поиска, здесь —
    /// в пользовательских свойствах сессии, которые облако раздаёт всем, кто смотрит список.
    /// Рассказывать о себе браузеру при этом нечем: свойства объявляются при создании
    /// комнаты и живут в StartGameArgs, поэтому IHostAdvertiser здесь и не реализован —
    /// в облаке этим занимается FusionSessionControl.
    public sealed class FusionHostBrowser : IHostBrowser
    {
        private const string OwnStackId = "fusion";
        private const string NoLobby = "Нет соединения с Photon";

        private readonly FusionRunnerFactory _runners;
        private readonly IHudMessages _hud;
        private readonly ReactiveProperty<IReadOnlyList<HostEntry>> _hosts = new(Array.Empty<HostEntry>());

        /// Вход в лобби — единственная операция стека, которая переживает свой scope:
        /// ответ от Photon приходит через сеть и может застать разобранный контейнер.
        private readonly CancellationTokenSource _life = new();

        private IDisposable _recycled;
        private bool _browsing;

        public Observable<IReadOnlyList<HostEntry>> Hosts => _hosts;

        public FusionHostBrowser(FusionRunnerFactory runners, IHudMessages hud)
        {
            _runners = runners;
            _hud = hud;
        }

        /// Отменяем здесь, а не в StopBrowsing: раннер у браузера и у сессии один и тот же,
        /// а отменённый вход в лобби Fusion доводит до Shutdown раннера — прекращение
        /// просмотра списка посреди матча убило бы сам матч. В Dispose уходит всё разом,
        /// и это единственный момент, когда такая отмена безопасна.
        ///
        /// Порядок VContainer здесь на нашей стороне: контейнер разбирает созданное стеком,
        /// то есть браузер гасится раньше фабрики раннера — и к моменту, когда та дёрнет
        /// Shutdown, продолжение входа в лобби уже никого не потревожит.
        public void Dispose()
        {
            StopBrowsing();
            _life.Cancel();
            _life.Dispose();
            _hosts.Dispose();
        }

        public void StartBrowsing()
        {
            if (_browsing) return;

            _browsing = true;

            /// Раннер одноразовый, и после каждого выхода из матча фабрика делает новый —
            /// в лобби облака приходится входить заново, иначе список навсегда замрёт
            /// на том, что было до матча.
            _recycled = _runners.Recycled.Subscribe(_ => JoinLobbyAsync().Forget());
            JoinLobbyAsync().Forget();
        }

        public void StopBrowsing()
        {
            _browsing = false;
            _recycled?.Dispose();
            _recycled = null;
        }

        /// Вызывает мост коллбэков. Список приходит целиком, поэтому и публикуем его целиком:
        /// ни склейки, ни истечения по TTL здесь нет — что прислало облако, то и есть правда.
        public void OnSessionListUpdated(List<SessionInfo> sessions)
        {
            var entries = new List<HostEntry>(sessions.Count);

            foreach (var session in sessions)
            {
                if (!session.IsOpen || !session.IsVisible) continue;

                entries.Add(new HostEntry(session.Name, session.PlayerCount, session.MaxPlayers, OwnStackId,
                    session.Name, ArenaOf(session)));
            }

            _hosts.Value = entries;
        }

        private static string ArenaOf(SessionInfo session)
        {
            if (session.Properties == null) return null;
            return session.Properties.TryGetValue(FusionSessionControl.ArenaKey, out var arena)
                ? (string)arena
                : null;
        }

        private async UniTaskVoid JoinLobbyAsync()
        {
            if (!_browsing) return;

            var result = await _runners.Ensure().JoinSessionLobby(SessionLobby.Shared, cancellationToken: _life.Token);

            /// Задача входа в лобби не завершается, пока раннер жив, — поэтому её ответ
            /// приходит ровно в момент разрушения, и без этой проверки продолжение
            /// упиралось бы в уничтоженный HUD и в уже закрытое свойство списка.
            if (!_browsing || result.Ok) return;

            /// Пустое лобби обязано быть подписано причиной: молчаливо пустой список
            /// на демонстрации читается как поломка.
            _hud.Show(NoLobby);
            _hosts.Value = Array.Empty<HostEntry>();
        }
    }
}
