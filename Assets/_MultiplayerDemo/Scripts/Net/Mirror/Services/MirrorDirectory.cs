using System;
using System.Collections.Generic;
using Mirror;
using R3;
using UnityEngine;

namespace Game.Net.Mirror
{
    /// У Mirror поиск в LAN штатный — в отличие от NGO, которому мы писали свой поверх
    /// UDP-широковещания. Это одна из строк сравнения в README, и здесь видно, чем именно она
    /// оборачивается: своего кода вчетверо меньше, зато чужой компонент диктует свои правила.
    ///
    /// Главное из них: поиск и объявление у Mirror — один компонент и один сокет.
    /// AdvertiseServer() первым делом зовёт StopDiscovery() (NetworkDiscoveryBase.cs:164),
    /// то есть хост перестаёт видеть чужие хосты. Поэтому режим здесь один из трёх и
    /// переключается по факту поднятого сервера: игрок, вернувшийся из матча в лобби, обязан
    /// снова увидеть список.
    ///
    /// Покадровый насос — ради того, чего у компонента нет: чистки протухших записей по TTL,
    /// живого счётчика игроков в ответе и переключения режима.
    public sealed class MirrorDirectory : IHostDirectory
    {
        /// После отказа сокета ждём секунду: спящий Wi-Fi, брандмауэр и занятый порт — поводы
        /// штатные, и повторять попытку каждый кадр незачем.
        private const double RETRY_INTERVAL = 1.0;

        private const string SOCKET_BROKEN = "Поиск Mirror: сеть отказала — ";

        private readonly HostRegistry _registry = new();
        private readonly HostListPublisher _publisher = new();
        private readonly MirrorDiscovery _discovery;

        private IDisposable _pump;
        private MirrorHostBeacon _beacon;
        private Mode _mode = Mode.Idle;
        private double _retryAt = double.NegativeInfinity;
        private bool _browsing;
        private bool _advertising;
        private bool _complained;

        public Observable<IReadOnlyList<HostEntry>> Hosts => _publisher.Hosts;

        public MirrorDirectory(MirrorDiscovery discovery)
        {
            _discovery = discovery;
            _discovery.HostFound += OnHostFound;
        }

        public void Dispose()
        {
            _browsing = false;
            _advertising = false;

            if (_discovery != null) _discovery.HostFound -= OnHostFound;

            Close();
            _publisher.Dispose();
        }

        public void StartBrowsing()
        {
            _browsing = true;
            Pump();
        }

        public void StopBrowsing()
        {
            _browsing = false;
            CloseIfIdle();
        }

        /// Зовёт сессия после успешного старта хоста: имя, места и метаданные объявления —
        /// те же, с которыми матч поднят. Счётчик игроков сюда не приходит, он живой (И-7).
        public void Advertise(SessionSettings settings)
        {
            _beacon = MirrorHostBeacon.Of(settings);
            _advertising = true;
            Pump();
        }

        public void StopAdvertising()
        {
            _advertising = false;
            CloseIfIdle();
        }

        /// Адрес у Mirror — это и есть запись: счётчики и метаданные взять неоткуда, ответ
        /// этого хоста мог и не дойти. Уровень клиент возьмёт из отмеченного в лобби.
        public bool TryParseManual(string text, out HostEntry entry)
        {
            entry = null;
            var fallback = MirrorAddress.PortOf(_discovery.transport);

            if (!MirrorAddress.TryParse(text, fallback, out var address, out var port)) return false;

            var token = MirrorAddress.Token(address, port);
            entry = new HostEntry(token, 0, 0, token, null);
            return true;
        }

        private void Pump()
        {
            if (_pump != null) return;

            _pump = Observable.EveryUpdate().Subscribe(_ => Tick());
        }

        private void CloseIfIdle()
        {
            if (_browsing || _advertising) return;

            Close();
        }

        private void Close()
        {
            _pump?.Dispose();
            _pump = null;
            _mode = Mode.Idle;

            if (_discovery != null) _discovery.StopDiscovery();
        }

        /// Публикуем и после отказа сокета: без этого протухшие записи остались бы в списке
        /// навсегда — TTL чистится только тем, кто спрашивает GetAlive.
        private void Tick()
        {
            var now = Time.realtimeSinceStartupAsDouble;

            ApplyMode(now);

            /// Счётчик игроков берём у сервера на каждом кадре: в чужом списке хост показывает,
            /// сколько народу у него сейчас, а не единицу с момента старта (И-7).
            if (_mode == Mode.Advertising)
            {
                _beacon.Players = NetworkServer.connections.Count;
                _discovery.Describe(_beacon);
            }

            if (_browsing) _publisher.Publish(_registry.GetAlive(now));
        }

        /// Ловим любое исключение, а не только SocketException: сокет открывает чужой
        /// компонент, и чем именно он отвечает на занятый порт или уснувший интерфейс, мы не
        /// решаем. Упавший насос выключил бы поиск навсегда — ровно то, что запрещает И-5.
        private void ApplyMode(double now)
        {
            var desired = Desired();
            if (desired == _mode || now < _retryAt) return;

            try
            {
                Switch(desired);
                _mode = desired;
            }
            catch (Exception exception)
            {
                _retryAt = now + RETRY_INTERVAL;
                Fail(exception);
            }
        }

        private Mode Desired()
        {
            if (_advertising && NetworkServer.active) return Mode.Advertising;

            return _browsing ? Mode.Browsing : Mode.Idle;
        }

        private void Switch(Mode mode)
        {
            switch (mode)
            {
                case Mode.Advertising:
                    _discovery.AdvertiseServer();
                    return;
                case Mode.Browsing:
                    _discovery.StartDiscovery();
                    return;
                default:
                    _discovery.StopDiscovery();
                    return;
            }
        }

        /// Жалуемся один раз — иначе одна потерянная сеть залила бы консоль строкой в секунду.
        private void Fail(Exception exception)
        {
            if (_complained) return;

            _complained = true;
            Debug.LogWarning($"{SOCKET_BROKEN}{exception.Message}. Поиск и объявление продолжатся сами.");
        }

        private void OnHostFound(MirrorHostBeacon beacon)
        {
            var token = MirrorAddress.Token(beacon.EndPoint.Address.ToString(), beacon.Port);
            var entry = new HostEntry(beacon.HostName, beacon.Players, beacon.MaxPlayers, token, beacon.Metadata());

            _registry.Report(entry, Time.realtimeSinceStartupAsDouble);
        }

        /// Сокет один, и режимы взаимоисключающие — это устройство компонента, а не наш выбор.
        private enum Mode
        {
            Idle,
            Browsing,
            Advertising,
        }
    }
}
