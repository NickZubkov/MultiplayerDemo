using System;
using System.Collections.Generic;
using Mirror;
using R3;

namespace Game.Net.Mirror
{
    /// Игроки сессии Mirror (спека § 5.2). Номер выдаёт сервер по порядку подключения:
    /// connectionId у KCP — хеш адреса и бывает отрицательным (спайк), а PlayerId отрицательных
    /// не принимает. У сервера — соответствие «подключение ↔ игрок»; у клиента — свой номер из
    /// приветствия и чужие по их аватарам.
    public sealed class MirrorPlayers : IDisposable
    {
        private readonly Dictionary<int, PlayerId> _byConnection = new();
        private readonly Dictionary<PlayerId, NetworkConnectionToClient> _connections = new();
        private readonly HashSet<PlayerId> _present = new();
        private readonly Subject<PlayerId> _joined = new();
        private readonly Subject<PlayerId> _left = new();

        private int _next;

        public PlayerId Local { get; private set; }
        public IReadOnlyCollection<PlayerId> All => _present;
        public Observable<PlayerId> Joined => _joined;
        public Observable<PlayerId> Left => _left;

        public void Dispose()
        {
            _joined.Dispose();
            _left.Dispose();
        }

        /// Сервер: подключение получило номер. У хоста его собственное подключение — первое,
        /// и номер ноль достаётся ему.
        public PlayerId Admit(NetworkConnectionToClient connection)
        {
            var player = new PlayerId(_next++);

            _byConnection[connection.connectionId] = player;
            _connections[player] = connection;

            if (connection is LocalConnectionToClient) Local = player;

            Appear(player);
            return player;
        }

        public void Release(NetworkConnectionToClient connection)
        {
            if (!_byConnection.Remove(connection.connectionId, out var player)) return;

            _connections.Remove(player);
            Vanish(player);
        }

        public PlayerId PlayerOf(NetworkConnectionToClient connection) =>
            connection != null && _byConnection.TryGetValue(connection.connectionId, out var player)
                ? player
                : PlayerId.NONE;

        public bool TryGetConnection(PlayerId player, out NetworkConnectionToClient connection) =>
            _connections.TryGetValue(player, out connection);

        /// Клиент: свой номер — из приветствия сервера.
        public void Greet(PlayerId local)
        {
            Local = local;
            Appear(local);
        }

        /// Клиент: чужой игрок виден по его аватару — у каждого игрока аватар ровно один.
        public void Appear(PlayerId player)
        {
            if (_present.Add(player)) _joined.OnNext(player);
        }

        public void Vanish(PlayerId player)
        {
            if (_present.Remove(player)) _left.OnNext(player);
        }

        /// Конец сессии: следующая раздаёт номера заново.
        public void Reset()
        {
            _byConnection.Clear();
            _connections.Clear();
            _present.Clear();
            _next = 0;
            Local = PlayerId.NONE;
        }
    }
}
