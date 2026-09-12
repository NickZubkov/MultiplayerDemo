using Game.Core;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Game.Net.Ngo
{
    /// Авторитет — хост: он решает, кто держит предмет, и он же считает физику,
    /// пока предмет свободен. На время удержания владение уходит держателю —
    /// иначе предмет в руках отстаёт от руки на RTT (решение D5 спеки).
    ///
    /// В руке предмет ведёт иерархия, а не репликация позиции: смену родителя NGO
    /// переносит сама, а само смещение в руке никуда не передаётся — оно константа
    /// префаба, и каждая машина ставит предмет в руку сама (см. LateUpdate).
    /// Родителем может быть только объект с NetworkObject, поэтому предмет цепляется
    /// к корню игрока, а точку крепления спрашивает у него отдельно.
    [RequireComponent(typeof(NetworkObject), typeof(Rigidbody))]
    public sealed class NgoItem : NetworkBehaviour
    {
        /// Предмет свободен. Идентификатором клиента ulong.MaxValue не бывает.
        private const ulong NO_HOLDER = ulong.MaxValue;

        private readonly NetworkVariable<ulong> _holder = new(NO_HOLDER);

        /// Автомат живёт только на сервере: переходы разрешает он, остальные видят результат.
        private readonly ItemState _state = new();

        private Rigidbody _body;
        private IHudMessages _hud;
        private Transform _attachedTo;
        private NgoPlayer _attachedPlayer;

        [Inject]
        public void Construct(IHudMessages hud) => _hud = hud;

        private void Awake() => _body = GetComponent<Rigidbody>();

        public override void OnNetworkSpawn()
        {
            _holder.OnValueChanged += OnHolderChanged;

            if (IsServer)
            {
                NetworkManager.OnClientDisconnectCallback += OnHolderMayHaveLeft;
            }

            /// Опоздавший клиент получает предмет уже занятым — состояние применяем сразу.
            OnHolderChanged(NO_HOLDER, _holder.Value);
        }

        public override void OnNetworkDespawn()
        {
            _holder.OnValueChanged -= OnHolderChanged;

            if (IsServer)
            {
                NetworkManager.OnClientDisconnectCallback -= OnHolderMayHaveLeft;
            }
        }

        /// Владение приезжает своим сообщением, держатель — переменной, и порядок
        /// их прихода не гарантирован: пересчитываем физику на каждом из событий.
        public override void OnGainedOwnership() => ApplyPhysicsState();

        public override void OnLostOwnership() => ApplyPhysicsState();

        [Rpc(SendTo.Server)]
        public void RequestPickRpc(RpcParams rpcParams = default)
        {
            var requester = rpcParams.Receive.SenderClientId;
            var player = FindPlayer(requester);

            var query = new PickupQuery(
                itemFree: _state.Phase == ItemPhase.Free,
                handsEmpty: player != null && !player.HandsBusy,
                distance: player == null ? float.MaxValue : Vector3.Distance(player.transform.position, transform.position),
                hasLineOfSight: true);   // луч уже проверен на клиенте, серверу хватает дистанции

            var denial = PickupRules.Evaluate(query);

            if (denial == PickupDenial.None && _state.TryHold(requester))
            {
                NetworkObject.ChangeOwnership(requester);
                _holder.Value = requester;
                return;
            }

            /// Отказ без причины возможен только если автомат уже отдал предмет:
            /// два запроса в одном кадре сервер разбирает по очереди, второму — «занято».
            var reason = denial == PickupDenial.None ? PickupDenial.ItemHeld : denial;
            DeniedRpc(reason, RpcTarget.Single(requester, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.Server)]
        public void RequestDropRpc(Vector3 impulse, RpcParams rpcParams = default)
        {
            var requester = rpcParams.Receive.SenderClientId;
            if (!_state.TryRelease(requester)) return;

            NetworkObject.RemoveOwnership();
            _holder.Value = NO_HOLDER;
            _body.AddForce(impulse, ForceMode.Impulse);
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void DeniedRpc(PickupDenial denial, RpcParams rpcParams) => _hud.Show(PickupDenialText.Describe(denial));

        /// Держатель ушёл из игры. Владение NGO вернула серверу сама, когда разбирала
        /// объекты ушедшего клиента, — нам остаётся снять держателя, и предмет упадёт
        /// там, где была его рука.
        private void OnHolderMayHaveLeft(ulong clientId)
        {
            if (_holder.Value != clientId) return;

            _state.TryRelease(clientId);
            _holder.Value = NO_HOLDER;
        }

        private void OnHolderChanged(ulong previous, ulong current)
        {
            /// previous нужен, чтобы освободить руки прежнему держателю.
            FindPlayer(previous)?.SetHeldItem(null);
            FindPlayer(current)?.SetHeldItem(this);

            if (IsServer)
            {
                ApplyAttachment(current);
            }

            ApplyPhysicsState();
        }

        /// Родителя меняет только сервер: клиентам NGO присылает готовую смену сама,
        /// а их собственный вызов TrySetParent всё равно вернул бы false.
        private void ApplyAttachment(ulong holder)
        {
            if (holder == NO_HOLDER)
            {
                if (transform.parent != null)
                {
                    NetworkObject.TryRemoveParent();
                }

                return;
            }

            var player = FindPlayer(holder);
            if (player == null) return;

            /// worldPositionStays: предмет въезжает в руку из точки, где лежал, и —
            /// важнее — остаётся на месте, когда объект держателя исчезает вместе с ним.
            NetworkObject.TrySetParent(player.NetworkObject, worldPositionStays: true);
        }

        /// Физику считает тот, кто владеет трансформом: свободный предмет — сервер,
        /// остальные повторяют за ним. В руке предмет ведёт иерархия, и кинематика нужна всем.
        private void ApplyPhysicsState()
        {
            var held = _holder.Value != NO_HOLDER;

            _body.isKinematic = held || !IsOwner;

            /// Предмет в руке торчит перед капсулой: с включёнными столкновениями
            /// он упирался бы в стены раньше самого держателя.
            _body.detectCollisions = !held;
        }

        /// Позу в руке каждый считает у себя, а не получает по сети. Смещение — константа
        /// префаба, одинаковая на всех машинах, а едет предмет вместе с трансформом держателя,
        /// потому что подцеплен к нему в иерархии. Репликация тут не нужна вовсе: она добавляла
        /// бы задержку и расхождения на ровном месте (откат «следование за точкой крепления», D7).
        ///
        /// Держателя спрашиваем у родителя, а не у NetworkManager: чужие объекты игроков
        /// клиенту искать нельзя, а родитель — это и есть объект держателя.
        private void LateUpdate()
        {
            var parent = transform.parent;
            if (parent == null) return;

            if (parent != _attachedTo)
            {
                _attachedTo = parent;
                _attachedPlayer = parent.GetComponentInParent<NgoPlayer>();
            }

            if (_attachedPlayer == null) return;

            transform.SetLocalPositionAndRotation(_attachedPlayer.HoldLocalPosition, _attachedPlayer.HoldLocalRotation);
        }

        /// Чужие объекты игроков видит только сервер — клиенту NGO отдаёт лишь его
        /// собственный. Больше и не нужно: руки и точку крепления спрашивают либо
        /// на сервере, либо у себя.
        private NgoPlayer FindPlayer(ulong clientId)
        {
            if (clientId == NO_HOLDER) return null;
            if (!IsServer && clientId != NetworkManager.LocalClientId) return null;

            var player = NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId);
            return player == null ? null : player.GetComponent<NgoPlayer>();
        }
    }
}
