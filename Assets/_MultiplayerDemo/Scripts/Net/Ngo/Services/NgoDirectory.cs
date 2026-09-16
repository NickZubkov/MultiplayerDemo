using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using R3;
using Unity.Netcode;
using UnityEngine;

namespace Game.Net.Ngo
{
    /// У NGO нет штатного поиска в LAN — свой поверх UDP-широковещания (у Mirror он есть из
    /// коробки: одна из строк сравнения в README).
    ///
    /// Поиск хостов — задача про время: пакеты приходят потоком, записи истекают по TTL,
    /// наружу уходит изменившийся список. Отсюда покадровый насос: приём идёт каждый кадр,
    /// маяк — раз в секунду, чистка протухших достаётся бесплатно от HostRegistry.GetAlive.
    ///
    /// Приём и объявление включаются порознь. Поиск нужен, пока открыто лобби; маяк — пока идёт
    /// матч, то есть ровно тогда, когда лобби уже закрыто. Общий насос у них один, но выключение
    /// одного не гасит другое.
    public sealed class NgoDirectory : IHostDirectory
    {
        /// Маяк вчетверо чаще TTL записи: хост успевает подтвердиться дважды,
        /// прежде чем одна потерянная датаграмма уронит его из чужого списка.
        private const double BEACON_INTERVAL = 1.0;

        private const string SOCKET_BROKEN = "Маяк NGO: сеть отказала — ";

        private readonly HostRegistry _registry = new();
        private readonly HostListPublisher _publisher = new();
        private readonly NetworkStackDefinition _definition;
        private readonly NetworkManager _manager;

        private LanBeaconSocket _socket;
        private IDisposable _pump;
        private SessionSettings _advertised;
        private string _localAddress;
        private double _lastBeaconAt = double.NegativeInfinity;
        private double _lastOpenAt = double.NegativeInfinity;
        private bool _browsing;
        private bool _complained;

        public Observable<IReadOnlyList<HostEntry>> Hosts => _publisher.Hosts;

        public NgoDirectory(NetworkStackDefinition definition, NetworkManager manager)
        {
            _definition = definition;
            _manager = manager;
        }

        public void Dispose()
        {
            _browsing = false;
            _advertised = null;
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
            _advertised = settings;
            _lastBeaconAt = double.NegativeInfinity;
            Pump();
        }

        public void StopAdvertising()
        {
            _advertised = null;
            CloseIfIdle();
        }

        /// Адрес у NGO — это и есть запись: счётчики и метаданные взять неоткуда, маяк
        /// этого хоста мог и не дойти. Уровень клиент возьмёт из отмеченного в лобби.
        public bool TryParseManual(string text, out HostEntry entry)
        {
            entry = null;
            if (!NgoAddress.TryParse(text, out var address, out var port)) return false;

            var token = NgoAddress.Token(address, port);
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
            if (_browsing || _advertised != null) return;

            Close();
        }

        private void Close()
        {
            _pump?.Dispose();
            _pump = null;
            _socket?.Dispose();
            _socket = null;
        }

        /// Публикуем и после отказа сокета: без этого протухшие записи остались бы в списке
        /// навсегда — TTL чистится только тем, кто спрашивает GetAlive.
        private void Tick()
        {
            var now = Time.realtimeSinceStartupAsDouble;

            try
            {
                Exchange(now);
            }
            catch (SocketException exception)
            {
                Fail(exception);
            }

            if (_browsing) _publisher.Publish(_registry.GetAlive(now));
        }

        /// Сокет открывается на насосе, а не при старте: и открытие, и приём, и отправка падают
        /// на штатных поводах — спящий Wi-Fi, брандмауэр, VPN, переезд в другую сеть, — и ни один
        /// из них не должен выключить поиск навсегда (И-5). Повтор открытия — не чаще маяка.
        private void Exchange(double now)
        {
            if (_socket == null)
            {
                if (now - _lastOpenAt < BEACON_INTERVAL) return;

                _lastOpenAt = now;
                _socket = new LanBeaconSocket();
            }

            if (_browsing) Drain(now);
            if (_advertised != null && now - _lastBeaconAt >= BEACON_INTERVAL) SendBeacon(now);
        }

        /// Сокет после отказа закрываем: спящий Wi-Fi и переезд в другую сеть оставляют его
        /// привязанным к исчезнувшему интерфейсу. Жалуемся один раз — иначе одна потерянная
        /// сеть залила бы консоль шестьюдесятью строками в секунду.
        private void Fail(SocketException exception)
        {
            _socket?.Dispose();
            _socket = null;

            if (_complained) return;

            _complained = true;
            Debug.LogWarning($"{SOCKET_BROKEN}{exception.Message}. Поиск и маяк продолжатся сами.");
        }

        /// Чужие маяки отсекаем по идентификатору стека из ассета (И-17): в LAN может шуметь
        /// соседнее окно на Mirror, а его адрес нашему транспорту не подойдёт.
        private void Drain(double now)
        {
            while (_socket.TryReceive(out var payload))
            {
                if (LanBeaconCodec.TryDecode(payload, out var stackId, out var entry) &&
                    stackId == _definition.StackId)
                {
                    _registry.Report(entry, now);
                }
            }
        }

        /// Счётчик игроков берётся у менеджера на каждой отправке: в чужом списке хост
        /// показывает, сколько народу у него сейчас, а не единицу с момента старта (И-7).
        private void SendBeacon(double now)
        {
            _lastBeaconAt = now;

            var entry = new HostEntry(_advertised.Name, _manager.ConnectedClientsIds.Count, _advertised.MaxPlayers,
                NgoAddress.Token(LocalAddress(), NgoAddress.DEFAULT_PORT), _advertised.Metadata);

            _socket.Send(LanBeaconCodec.Encode(_definition.StackId, entry));
        }

        /// Адрес спрашиваем однажды: Dns.GetHostAddresses ходит в системный резолвер, а маяк
        /// уходит каждую секунду из главного потока. Запасное значение кладём до опроса —
        /// тогда и упавший резолвер оставит в кэше адрес, а не повод спрашивать снова.
        private string LocalAddress()
        {
            if (_localAddress != null) return _localAddress;

            _localAddress = NgoAddress.LOCALHOST;

            foreach (var address in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    _localAddress = address.ToString();
                    break;
                }
            }

            return _localAddress;
        }
    }
}
