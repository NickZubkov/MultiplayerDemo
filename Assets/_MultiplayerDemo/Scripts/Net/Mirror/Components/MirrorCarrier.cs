using Mirror;
using UnityEngine;

namespace Game.Net.Mirror
{
    /// Носитель Mirror (спека § 5.10): блок состояния — в [SyncVar], команды — [Command] к
    /// серверу, события — [TargetRpc] и [ClientRpc]. Всё идёт через сервер, и он же переводит
    /// подключение в PlayerId: номеров чужих подключений у клиента нет.
    ///
    /// Авторитет сущностей мира — сервер, аватара — его владелец; у носителя на Player_Mirror
    /// поэтому syncDirection ClientToServer, у остальных — ServerToClient (выставлено в префабах).
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class MirrorCarrier : NetworkBehaviour, INetCarrier
    {
        [SyncVar(hook = nameof(OnStateChanged))] private MirrorBlob _state;
        [SyncVar] private ulong _sceneEntity;

        /// Value владельца; −1 — «никто», так же пишет NetWriter (15.5).
        [SyncVar] private int _owner = -1;

        private INetWorld _world;
        private MirrorPlayers _players;
        private NetEntityChannel _channel;
        private NetEntity _bound;
        private NetworkTransformReliable _transform;

        public NetEntityId Id => _sceneEntity != 0 ? new NetEntityId(_sceneEntity) : NetEntityId.Dynamic(netId);

        public PlayerId Owner => ToPlayer(_owner);

        public bool IsAuthority => Owner.IsNone ? isServer : isOwned;

        private void Awake() => _transform = GetComponent<NetworkTransformReliable>();

        /// Зовёт спавнер сразу после создания экземпляра — у сервера и у клиента.
        public void UseWorld(INetWorld world, MirrorPlayers players)
        {
            _world = world;
            _players = players;
        }

        /// Зовёт спавнер у сервера до NetworkServer.Spawn: значения уйдут в сообщении о спавне.
        public void Mark(NetEntityId sceneEntity, PlayerId owner)
        {
            _sceneEntity = sceneEntity.IsNone ? 0 : sceneEntity.Value;
            _owner = owner.IsNone ? -1 : owner.Value;
        }

        /// У хоста придут и OnStartServer, и OnStartClient — канал открывается один раз.
        public override void OnStartServer() => Open();

        public override void OnStartClient()
        {
            if (!isServer && !Owner.IsNone) _players.Appear(Owner);

            Open();
        }

        public override void OnStopClient()
        {
            if (!isServer && !Owner.IsNone) _players.Vanish(Owner);

            Close();
        }

        public override void OnStopServer() => Close();

        public void PublishState(in NetBlob state) => _state = MirrorBlob.From(state);

        public void SendToAuthority(uint type, in NetBlob payload)
        {
            if (IsAuthority)
            {
                _channel.Receive(_players.Local, type, payload);
                return;
            }

            CmdToAuthority(type, MirrorBlob.From(payload));
        }

        public void SendToPlayer(PlayerId target, uint type, in NetBlob payload)
        {
            if (target == _players.Local)
            {
                _channel.Receive(_players.Local, type, payload);
                return;
            }

            if (isServer)
            {
                Deliver(target, _players.Local, type, MirrorBlob.From(payload));
                return;
            }

            CmdToPlayer(target.Value, type, MirrorBlob.From(payload));
        }

        public void SendToAll(uint type, in NetBlob payload)
        {
            if (isServer)
            {
                RpcToAll(_players.Local.Value, type, MirrorBlob.From(payload));
                return;
            }

            CmdToAll(type, MirrorBlob.From(payload));
        }

        /// ServerTeleport объявляет разрыв и клиентам — RpcTeleport изнутри того же вызова.
        public void Snap()
        {
            if (!isServer || _transform == null) return;

            _transform.ServerTeleport(transform.position, transform.rotation);
        }

        /// Команду сущности мира сервер исполняет сам; команду аватару — у владельца.
        [Command(requiresAuthority = false)]
        private void CmdToAuthority(uint type, MirrorBlob payload, NetworkConnectionToClient sender = null)
        {
            var from = _players.PlayerOf(sender);

            if (Owner.IsNone)
            {
                _channel.Receive(from, type, payload.ToNet());
                return;
            }

            Deliver(Owner, from, type, payload);
        }

        [Command(requiresAuthority = false)]
        private void CmdToPlayer(int target, uint type, MirrorBlob payload, NetworkConnectionToClient sender = null) =>
            Deliver(ToPlayer(target), _players.PlayerOf(sender), type, payload);

        [Command(requiresAuthority = false)]
        private void CmdToAll(uint type, MirrorBlob payload, NetworkConnectionToClient sender = null) =>
            RpcToAll(_players.PlayerOf(sender).Value, type, payload);

        [TargetRpc]
        private void TargetDeliver(NetworkConnectionToClient target, int sender, uint type, MirrorBlob payload) =>
            _channel?.Receive(ToPlayer(sender), type, payload.ToNet());

        [ClientRpc]
        private void RpcToAll(int sender, uint type, MirrorBlob payload) =>
            _channel?.Receive(ToPlayer(sender), type, payload.ToNet());

        private void Deliver(PlayerId target, PlayerId sender, uint type, MirrorBlob payload)
        {
            if (!_players.TryGetConnection(target, out var connection)) return;

            TargetDeliver(connection, sender.Value, type, payload);
        }

        /// Хук у клиента при спавне зовётся раньше OnStartClient (спайк) — канала ещё нет,
        /// текущее значение заберёт Open. Пишущий получает хук тоже — ему канал уже раздал.
        private void OnStateChanged(MirrorBlob previous, MirrorBlob current)
        {
            if (_channel == null || IsAuthority) return;

            _channel.ReceiveState(current.ToNet());
        }

        private void Open()
        {
            if (_channel != null) return;

            _channel = new NetEntityChannel(this);

            if (_state.Length > 0) _channel.ReceiveState(_state.ToNet());

            _bound = Target();
            if (_bound != null) _bound.Bind(_channel);
        }

        private void Close()
        {
            if (_channel == null) return;

            if (_bound != null) _bound.Bind(null);
            _bound = null;

            _channel.Dispose();
            _channel = null;
        }

        private NetEntity Target()
        {
            if (_sceneEntity == 0) return GetComponent<NetEntity>();

            if (_world != null && _world.TryGetSceneEntity(Id, out var entity)) return entity;

            Debug.LogError($"Носитель Mirror {Id}: в арене нет сценовой сущности с таким идентификатором");
            return null;
        }

        private static PlayerId ToPlayer(int value) => value < 0 ? PlayerId.NONE : new PlayerId(value);
    }
}
