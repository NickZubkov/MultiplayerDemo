using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Game.Net.Ngo
{
    /// Носитель NGO (спека § 5.10): блок состояния — в NetworkVariable, команды — RPC владельцу
    /// объекта, события — RPC игроку или всем. Про игру не знает: байты переносит, отправителя
    /// узнаёт из RPC и отдаёт каналу.
    ///
    /// Авторитет — владелец объекта у NGO: сущности мира принадлежат серверу, аватар — своему
    /// игроку. Поэтому и запись состояния — Owner, и команды — SendTo.Owner: одно правило на
    /// оба вида сущностей, без ветвления.
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NgoCarrier : NetworkBehaviour, INetCarrier
    {
        private readonly NetworkVariable<NgoBlob> _state =
            new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// Ноль — сущность динамическая; иначе значение NetEntityId сценовой сущности.
        private readonly NetworkVariable<ulong> _sceneEntity = new();

        private INetWorld _world;
        private NetEntityId _markedScene;
        private NetEntityChannel _channel;
        private NetEntity _bound;
        private NetworkTransform _transform;

        public NetEntityId Id =>
            _sceneEntity.Value != 0 ? new NetEntityId(_sceneEntity.Value) : NetEntityId.Dynamic(NetworkObjectId);

        public PlayerId Owner => NetworkObject.IsPlayerObject ? NgoIds.Player(OwnerClientId) : PlayerId.NONE;

        public bool IsAuthority => IsOwner;

        private PlayerId Me => NgoIds.Player(NetworkManager.LocalClientId);

        private void Awake() => _transform = GetComponent<NetworkTransform>();

        /// Зовёт спавнер сразу после создания экземпляра — на любой машине, до спавна.
        public void UseWorld(INetWorld world) => _world = world;

        /// Зовёт спавнер у судьи до Spawn: какую сценовую сущность этот носитель представляет.
        public void MarkScene(NetEntityId sceneEntity) => _markedScene = sceneEntity;

        /// Значение, записанное сервером здесь, уходит клиентам в самом сообщении о спавне.
        public override void OnNetworkSpawn()
        {
            if (IsServer && !_markedScene.IsNone) _sceneEntity.Value = _markedScene.Value;

            _state.OnValueChanged += OnStateChanged;
            _channel = new NetEntityChannel(this);

            /// Уже приехавшее состояние — в канал до привязки: подписчик Bound строит логику,
            /// и её OnState обязан сразу увидеть текущее — так поздний клиент застаёт ящик
            /// уже в чужих руках.
            if (_state.Value.Length > 0) _channel.ReceiveState(_state.Value.ToNet());

            _bound = Target();
            if (_bound != null) _bound.Bind(_channel);
        }

        public override void OnNetworkDespawn()
        {
            _state.OnValueChanged -= OnStateChanged;

            if (_bound != null) _bound.Bind(null);
            _bound = null;

            _channel?.Dispose();
            _channel = null;
        }

        public void PublishState(in NetBlob state) => _state.Value = NgoBlob.From(state);

        public void SendToAuthority(uint type, in NetBlob payload)
        {
            if (IsAuthority)
            {
                _channel.Receive(Me, type, payload);
                return;
            }

            ToAuthorityRpc(type, NgoBlob.From(payload));
        }

        public void SendToPlayer(PlayerId target, uint type, in NetBlob payload)
        {
            if (target == Me)
            {
                _channel.Receive(Me, type, payload);
                return;
            }

            ToPlayerRpc(type, NgoBlob.From(payload), RpcTarget.Single(NgoIds.Client(target), RpcTargetUse.Temp));
        }

        public void SendToAll(uint type, in NetBlob payload) => ToAllRpc(type, NgoBlob.From(payload));

        /// Разрыв движения объявляет тот, кто ведёт позу; у сценового носителя позы нет.
        public void Snap()
        {
            if (!IsAuthority || _transform == null) return;

            _transform.Teleport(transform.position, transform.rotation, transform.localScale);
        }

        [Rpc(SendTo.Owner)]
        private void ToAuthorityRpc(uint type, NgoBlob payload, RpcParams rpcParams = default) =>
            _channel?.Receive(NgoIds.Player(rpcParams.Receive.SenderClientId), type, payload.ToNet());

        [Rpc(SendTo.SpecifiedInParams)]
        private void ToPlayerRpc(uint type, NgoBlob payload, RpcParams rpcParams) =>
            _channel?.Receive(NgoIds.Player(rpcParams.Receive.SenderClientId), type, payload.ToNet());

        [Rpc(SendTo.Everyone)]
        private void ToAllRpc(uint type, NgoBlob payload, RpcParams rpcParams = default) =>
            _channel?.Receive(NgoIds.Player(rpcParams.Receive.SenderClientId), type, payload.ToNet());

        /// OnValueChanged зовётся и у пишущего (спайк, вопрос 3), а ему канал состояние уже
        /// раздал из SetState.
        private void OnStateChanged(NgoBlob previous, NgoBlob current)
        {
            if (IsAuthority || _channel == null) return;

            _channel.ReceiveState(current.ToNet());
        }

        private NetEntity Target()
        {
            if (_sceneEntity.Value == 0) return GetComponent<NetEntity>();

            if (_world != null && _world.TryGetSceneEntity(Id, out var entity)) return entity;

            Debug.LogError($"Носитель NGO {Id}: в арене нет сценовой сущности с таким идентификатором");
            return null;
        }
    }
}
