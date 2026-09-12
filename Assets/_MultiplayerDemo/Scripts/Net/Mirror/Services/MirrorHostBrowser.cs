using System;
using System.Collections.Generic;
using Game.Core;
using Mirror;
using R3;

namespace Game.Net.Mirror
{
    /// У Mirror поиск в LAN штатный — в отличие от NGO, которому мы писали свой поверх
    /// UDP-широковещания. Это одна из строк сравнения в README, и здесь видно, чем
    /// именно она оборачивается: своего кода остаётся вчетверо меньше, зато чужой
    /// компонент диктует свои правила.
    ///
    /// Главное из них: поиск и объявление у Mirror — один компонент и один сокет.
    /// AdvertiseServer() первым делом зовёт StopDiscovery() (NetworkDiscoveryBase.cs:164),
    /// то есть хост перестаёт видеть чужие хосты. Поэтому режим переключаем по факту
    /// поднятого сервера, а не по вызову Advertise: игрок, вернувшийся из матча в лобби,
    /// обязан снова увидеть список.
    public sealed class MirrorHostBrowser : IHostBrowser, IHostAdvertiser
    {
        /// Своими считаем только хосты Mirror. Проверка почти формальная — у Mirror свой
        /// порт и своё рукопожатие, — но StackId едет в HostEntry и без неё был бы пуст.
        private const string OWN_STACK_ID = "mirror";

        private readonly MirrorDiscovery _discovery;
        private readonly IClock _clock;
        private readonly HostRegistry _registry = new();
        private readonly ReactiveProperty<IReadOnlyList<HostEntry>> _hosts = new(Array.Empty<HostEntry>());

        private IDisposable _pump;
        private string _hostName;
        private bool _advertising;

        public Observable<IReadOnlyList<HostEntry>> Hosts => _hosts;

        public MirrorHostBrowser(MirrorDiscovery discovery, IClock clock)
        {
            _discovery = discovery;
            _clock = clock;
        }

        public void Dispose()
        {
            StopBrowsing();
            _hosts.Dispose();
        }

        public void StartBrowsing()
        {
            if (_pump != null) return;

            _discovery.HostFound += OnHostFound;
            _discovery.StartDiscovery();

            /// Насос нужен не ради приёма — ответы приносит сам Mirror, — а ради двух
            /// вещей, которых у него нет: чистки протухших записей по TTL и переключения
            /// режима, когда сессия поднялась или упала.
            _pump = Observable.EveryUpdate()
                              .Do(_ => ApplyMode())
                              .Select(_ => _registry.GetAlive(_clock.Now))
                              .Subscribe(Publish);
        }

        public void StopBrowsing()
        {
            if (_pump == null) return;

            _pump.Dispose();
            _pump = null;
            _discovery.HostFound -= OnHostFound;
            _advertising = false;

            /// Компонент живёт в сцене стека и гаснет вместе с ней сам (OnDisable →
            /// Shutdown), но выгрузка сцены может случиться позже — сокет держать незачем.
            if (_discovery != null) _discovery.StopDiscovery();
        }

        /// Вызывает презентер после успешного старта хоста. Само объявление включит
        /// ближайший кадр насоса — к этому моменту сервер уже поднят.
        public void Advertise(string hostName, int players, int maxPlayers, string arenaId)
        {
            _hostName = hostName;
            _discovery.Describe(new MirrorHostBeacon
            {
                HostName = hostName,
                Players = players,
                MaxPlayers = maxPlayers,
                ArenaId = arenaId
            });
        }

        private void ApplyMode()
        {
            var advertise = _hostName != null && NetworkServer.active;
            if (advertise == _advertising) return;

            _advertising = advertise;

            if (advertise) _discovery.AdvertiseServer();
            else _discovery.StartDiscovery();
        }

        private void OnHostFound(MirrorHostBeacon beacon)
        {
            var entry = new HostEntry(beacon.HostName, beacon.Players, beacon.MaxPlayers, OWN_STACK_ID,
                $"{beacon.EndPoint.Address}:{beacon.Port}", beacon.ArenaId);

            _registry.Report(entry, _clock.Now);
        }

        /// ReactiveProperty сравнивает значения по ссылке, а GetAlive каждый кадр отдаёт
        /// новый список — без этой проверки лобби перерисовывалось бы по шестьдесят раз
        /// в секунду. Та же пара методов есть у NgoHostBrowser; сводить их в общее место
        /// стоит на третьей копии, в задаче про Fusion.
        private void Publish(IReadOnlyList<HostEntry> hosts)
        {
            if (SameAsPublished(hosts)) return;
            _hosts.Value = hosts;
        }

        private bool SameAsPublished(IReadOnlyList<HostEntry> hosts)
        {
            var published = _hosts.CurrentValue;
            if (published.Count != hosts.Count) return false;

            for (var i = 0; i < hosts.Count; i++)
            {
                if (published[i].JoinToken != hosts[i].JoinToken) return false;
                if (published[i].Players != hosts[i].Players) return false;
                if (published[i].Name != hosts[i].Name) return false;
            }

            return true;
        }
    }
}
