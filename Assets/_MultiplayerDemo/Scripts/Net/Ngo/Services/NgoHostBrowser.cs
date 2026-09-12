using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Game.Core;
using R3;

namespace Game.Net.Ngo
{
    /// У NGO нет штатного поиска в LAN — пишем свой поверх UDP-широковещания
    /// (у Mirror он есть из коробки: одна из строк сравнения в README).
    ///
    /// Поиск хостов — задача про время: пакеты приходят потоком, записи истекают по TTL,
    /// наружу уходит изменившийся список. Поэтому здесь R3, а не ручная прокачка из
    /// презентера: приём идёт покадрово, отправка маяка — раз в секунду, чистка протухших
    /// достаётся бесплатно от HostRegistry.GetAlive.
    public sealed class NgoHostBrowser : IHostBrowser, IHostAdvertiser
    {
        /// Маяк вчетверо чаще TTL записи: хост успевает подтвердиться дважды,
        /// прежде чем одна потерянная датаграмма уронит его из чужого списка.
        private const double BEACON_INTERVAL = 1.0;

        /// Своим считаем только маяки NGO: в LAN может шуметь соседнее окно на Mirror,
        /// а его адрес нашему транспорту не подойдёт.
        private const string OWN_STACK_ID = "ngo";

        private readonly HostRegistry _registry = new();
        private readonly ReactiveProperty<IReadOnlyList<HostEntry>> _hosts = new(Array.Empty<HostEntry>());
        private readonly IClock _clock;

        private LanBeaconSocket _socket;
        private IDisposable _pump;
        private string _localAddress;
        private string _advertisedName;
        private string _advertisedArena;
        private int _players;
        private int _maxPlayers;
        private double _lastBeaconAt = double.NegativeInfinity;

        public Observable<IReadOnlyList<HostEntry>> Hosts => _hosts;

        public NgoHostBrowser(IClock clock)
        {
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
            _socket ??= new LanBeaconSocket();

            /// Один покадровый поток вместо приёма кадрами плюс Observable.Interval на маяк:
            /// Interval ходит по TimeProvider, а часы проекта живут за IClock — подменяемого
            /// TimeProvider под netstandard2.1 нет (Docs/Нулевой день.md § 4).
            _pump = Observable.EveryUpdate()
                              .Do(_ => Drain())
                              .Do(_ => SendBeaconIfDue())
                              .Select(_ => _registry.GetAlive(_clock.Now))
                              .Subscribe(Publish);
        }

        public void StopBrowsing()
        {
            _pump?.Dispose();
            _pump = null;
            _socket?.Dispose();
            _socket = null;
        }

        /// Вызывает презентер после успешного старта хоста. Первый маяк уйдёт в ближайшем
        /// кадре, но только пока идёт поиск: приём и отправка висят на одном насосе.
        public void Advertise(string hostName, int players, int maxPlayers, string arenaId)
        {
            _advertisedName = hostName;
            _players = players;
            _maxPlayers = maxPlayers;
            _advertisedArena = arenaId;
        }

        private void Drain()
        {
            while (_socket.TryReceive(out var payload))
            {
                if (LanBeaconCodec.TryDecode(payload, out var entry) && entry.StackId == OWN_STACK_ID)
                {
                    _registry.Report(entry, _clock.Now);
                }
            }
        }

        private void SendBeaconIfDue()
        {
            if (_advertisedName == null) return;
            if (_clock.Now - _lastBeaconAt < BEACON_INTERVAL) return;

            _lastBeaconAt = _clock.Now;
            var entry = new HostEntry(_advertisedName, _players, _maxPlayers, OWN_STACK_ID,
                $"{LocalAddress()}:{NgoSessionControl.PORT}", _advertisedArena);
            _socket.Send(LanBeaconCodec.Encode(entry));
        }

        /// ReactiveProperty сравнивает значения по ссылке, а GetAlive каждый кадр отдаёт
        /// новый список — без этой проверки лобби перерисовывалось бы по шестьдесят раз в секунду.
        private void Publish(IReadOnlyList<HostEntry> hosts)
        {
            if (SameAsPublished(hosts)) return;
            _hosts.Value = hosts;
        }

        /// Сравниваем по содержимому, а не по длине, как предполагал план: длина не меняется
        /// ни когда у хоста прибавился игрок, ни когда один хост сменился другим в том же кадре.
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

        /// Адрес спрашиваем однажды: Dns.GetHostAddresses ходит в системный резолвер,
        /// а маяк уходит каждую секунду из главного потока.
        private string LocalAddress()
        {
            if (_localAddress != null) return _localAddress;

            _localAddress = "127.0.0.1";

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
