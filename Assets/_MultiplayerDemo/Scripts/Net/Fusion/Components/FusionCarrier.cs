using Fusion;
using UnityEngine;

namespace Game.Net.Fusion
{
    /// Носитель Fusion (спека § 5.10): блок состояния — в [Networked], команды — RPC к
    /// StateAuthority, события — RPC игроку ([RpcTarget]) или всем.
    ///
    /// Авторитет — власть над состоянием: у сущностей мира — мастер-клиент (флаг
    /// MasterClientObject на префабе, Ц7), у аватара — его создатель. FixedUpdateNetwork
    /// носителю не нужен: в Shared Mode он идёт только у власти (спайк), а смену состояния
    /// видеть должны все — поэтому ChangeDetector в Render.
    [RequireComponent(typeof(NetworkObject))]
    public sealed class FusionCarrier : NetworkBehaviour, INetCarrier, IStateAuthorityChanged
    {
        private INetWorld _world;
        private ChangeDetector _changes;
        private NetEntityChannel _channel;
        private NetEntity _bound;
        private NetworkTransform _transform;
        private PlayerId _owner;

        [Networked] public FusionBlob Blob { get; set; }

        /// Ноль — сущность динамическая; иначе значение NetEntityId сценовой. Пишет судья в
        /// onBeforeSpawned — до рассылки объекта.
        [Networked] public ulong SceneEntity { get; set; }

        /// new — у NetworkBehaviour есть свой Id, номер поведения внутри объекта. Скрываем
        /// осознанно: наружу носитель показывает идентификатор сущности, как у NGO и Mirror,
        /// а внутренний номер Fusion игре не нужен.
        public new NetEntityId Id => SceneEntity != 0 ? new NetEntityId(SceneEntity) : NetEntityId.Dynamic(Object.Id.Raw);

        /// Владелец аватара — тот, кому спавнер отдал ввод; у сущностей мира ввода нет ни у кого.
        /// Запоминаем при спавне и держим полем: после Despawned Fusion обнуляет Object, а
        /// владельца у сущности спрашивают и позже — оболочка аватара снимается с реестра уже
        /// в своём OnDestroy.
        public PlayerId Owner => _owner;

        /// Object обнуляется вместе с концом жизни объекта, и спросить власть после этого —
        /// обычное дело: подписки логики живут до OnDestroy оболочки.
        public bool IsAuthority => Object != null && HasStateAuthority;

        private PlayerId Me => FusionIds.Player(Runner.LocalPlayer);

        private void Awake() => _transform = GetComponent<NetworkTransform>();

        /// Зовёт спавнер, когда провайдер объектов создаёт экземпляр, — на любой машине.
        public void UseWorld(INetWorld world) => _world = world;

        public override void Spawned()
        {
            _owner = FusionIds.Player(Object.InputAuthority);
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
            _channel = new NetEntityChannel(this);

            /// Уже приехавшее состояние — в канал до привязки: подписчик Bound строит логику,
            /// и её OnState обязан сразу увидеть текущее — так поздний клиент застаёт ящик
            /// уже в чужих руках.
            if (Blob.Length > 0) _channel.ReceiveState(Blob.ToNet());

            _bound = Target();
            if (_bound != null) _bound.Bind(_channel);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_bound != null) _bound.Bind(null);
            _bound = null;

            _channel?.Dispose();
            _channel = null;
        }

        /// ChangeDetector видит и собственную запись (спайк, вопрос 3) — пишущему канал
        /// состояние уже раздал из SetState.
        public override void Render()
        {
            if (_channel == null) return;

            foreach (var change in _changes.DetectChanges(this))
            {
                if (change == nameof(Blob) && !IsAuthority) _channel.ReceiveState(Blob.ToNet());
            }
        }

        /// Мастер-клиент ушёл — власть над миром у нового (спайк, вопрос 2), и логика
        /// перепроверяет своё состояние: держатель мог уйти вместе со старым судьёй.
        public void StateAuthorityChanged() => _channel?.RaiseAuthorityChanged();

        public void PublishState(in NetBlob state) => Blob = FusionBlob.From(state);

        public void SendToAuthority(uint type, in NetBlob payload)
        {
            if (IsAuthority)
            {
                _channel.Receive(Me, type, payload);
                return;
            }

            RpcToAuthority(type, FusionBlob.From(payload));
        }

        public void SendToPlayer(PlayerId target, uint type, in NetBlob payload)
        {
            if (target == Me)
            {
                _channel.Receive(Me, type, payload);
                return;
            }

            if (!TryGetRef(target, out var player)) return;

            RpcToPlayer(player, type, FusionBlob.From(payload));
        }

        public void SendToAll(uint type, in NetBlob payload) => RpcToAll(type, FusionBlob.From(payload));

        /// Teleport гасит интерполяцию между прошлым и текущим тиком — у себя и у всех
        /// (Fusion.Runtime.xml); у сценового носителя NetworkTransform нет.
        public void Snap()
        {
            if (!IsAuthority || _transform == null) return;

            _transform.Teleport(transform.position, transform.rotation);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RpcToAuthority(uint type, FusionBlob payload, RpcInfo info = default) =>
            _channel?.Receive(FusionIds.Player(info.Source), type, payload.ToNet());

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RpcToPlayer([RpcTarget] PlayerRef target, uint type, FusionBlob payload, RpcInfo info = default) =>
            _channel?.Receive(FusionIds.Player(info.Source), type, payload.ToNet());

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RpcToAll(uint type, FusionBlob payload, RpcInfo info = default) =>
            _channel?.Receive(FusionIds.Player(info.Source), type, payload.ToNet());

        /// PlayerRef по номеру — среди игроков сессии, а не арифметикой над индексом: как номер
        /// связан с внутренним индексом Fusion, стек не обещает.
        private bool TryGetRef(PlayerId target, out PlayerRef player)
        {
            foreach (var active in Runner.ActivePlayers)
            {
                if (active.PlayerId != target.Value) continue;

                player = active;
                return true;
            }

            player = PlayerRef.None;
            return false;
        }

        private NetEntity Target()
        {
            if (SceneEntity == 0) return GetComponent<NetEntity>();

            if (_world != null && _world.TryGetSceneEntity(Id, out var entity)) return entity;

            Debug.LogError($"Носитель Fusion {Id}: в арене нет сценовой сущности с таким идентификатором");
            return null;
        }
    }
}
