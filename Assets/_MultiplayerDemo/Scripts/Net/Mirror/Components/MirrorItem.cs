using Game.Core;
using Mirror;
using UnityEngine;
using VContainer;

namespace Game.Net.Mirror
{
    /// Авторитет — сервер: он решает, кто держит предмет, и он же считает физику,
    /// пока предмет свободен. Держатель едет одним SyncVar, а всё остальное каждая
    /// машина делает у себя по его хуку.
    ///
    /// **Владение держателю не передаём** — расхождение с планом, у которого здесь
    /// стояли AssignClientAuthority и RemoveClientAuthority. Причина жёсткая: Mirror
    /// при отключении клиента уничтожает всё, чем тот владел
    /// (NetworkConnectionToClient.DestroyOwnedObjects), и ящик в руках ушедшего игрока
    /// не упал бы на пол, а исчез — прямо против пункта 7 чек-листа приёмки.
    /// Владение и не нужно: команды объявлены requiresAuthority = false.
    ///
    /// В руке предмет ведёт иерархия, а не репликация позиции, и это сходится с
    /// NetworkTransform само собой: тот шлёт локальные координаты (CoordinateSpace.Local
    /// по умолчанию), а локальные координаты предмета в руке — нули на всех машинах.
    ///
    /// Рвётся только момент перехода: в буфере интерполяции остаются снимки от прошлой
    /// жизни предмета, и после броска он уезжал бы к началу координат — там лежат те
    /// самые нули, снятые пока он висел в руке. Поэтому на каждой смене держателя сервер
    /// зовёт ServerTeleport: это штатный способ Mirror объявить разрыв движения, и он
    /// симметричный — буфер снимков и состояние дельта-сжатия обнуляются и на сервере,
    /// и у клиентов в один и тот же момент. Выключать компонент вместо этого нельзя:
    /// OnDisable и OnEnable зовут ResetState в одностороннем порядке, и хватило бы одного
    /// пакета, разошедшегося с перезапуском дельты, чтобы предмет уехал навсегда.
    [RequireComponent(typeof(NetworkIdentity), typeof(NetworkTransformReliable), typeof(Rigidbody))]
    public sealed class MirrorItem : NetworkBehaviour
    {
        /// Предмет свободен. netId заспавненного объекта нулём не бывает.
        private const uint NO_HOLDER = 0;

        /// Автомат живёт только на сервере: переходы разрешает он, остальные видят результат.
        private readonly ItemState _state = new();

        [SyncVar(hook = nameof(OnHolderChanged))]
        private uint _holder;

        private Rigidbody _body;
        private NetworkTransformReliable _transformSync;
        private IHudMessages _hud;

        [Inject]
        public void Construct(IHudMessages hud) => _hud = hud;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _transformSync = GetComponent<NetworkTransformReliable>();
        }

        /// Опоздавший клиент получает предмет уже занятым: хук на начальной десериализации
        /// сработает только если значение отличается от нуля, поэтому состояние применяем
        /// здесь ещё раз — вызов идемпотентен.
        public override void OnStartClient() => Apply(_holder);

        /// Держатель ушёл из игры. Предметы Mirror сам не трогает — владение мы им не
        /// передаём, — поэтому отпускаем их мы, и обязательно до того, как база уничтожит
        /// объект игрока: после этого netId держателя уже не с чем сопоставить.
        public static void ReleaseAllHeldBy(uint holder)
        {
            if (holder == NO_HOLDER) return;

            foreach (var identity in NetworkServer.spawned.Values)
            {
                if (identity != null && identity.TryGetComponent<MirrorItem>(out var item))
                {
                    item.ReleaseIfHeldBy(holder);
                }
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdRequestPick(NetworkConnectionToClient sender = null)
        {
            var player = PlayerOf(sender);
            var requester = sender?.identity == null ? NO_HOLDER : sender.identity.netId;

            var query = new PickupQuery(
                itemFree: _state.Phase == ItemPhase.Free,
                handsEmpty: player != null && !player.HandsBusy,
                distance: player == null ? float.MaxValue : Vector3.Distance(player.transform.position, transform.position),
                hasLineOfSight: true);   // луч уже проверен на клиенте, серверу хватает дистанции

            var denial = PickupRules.Evaluate(query);

            if (denial == PickupDenial.None && requester != NO_HOLDER && _state.TryHold(requester))
            {
                _holder = requester;
                return;
            }

            /// Отказ без причины возможен только если автомат уже отдал предмет:
            /// два запроса в одном кадре сервер разбирает по очереди, второму — «занято».
            TargetDenied(sender, denial == PickupDenial.None ? PickupDenial.ItemHeld : denial);
        }

        [Command(requiresAuthority = false)]
        public void CmdRequestDrop(Vector3 impulse, NetworkConnectionToClient sender = null)
        {
            if (sender?.identity == null) return;
            if (!_state.TryRelease(sender.identity.netId)) return;

            /// Присвоение SyncVar на хосте синхронно зовёт хук (NetworkBehaviour.GeneratedSyncVarSetter
            /// делает это при NetworkServer.activeHost), то есть к следующей строке тело уже
            /// не кинематическое и импульс дойдёт. Сервер у нас всегда хост — выделенного
            /// в демке нет, и второй ветки под него мы не заводим.
            _holder = NO_HOLDER;
            _body.AddForce(impulse, ForceMode.Impulse);
        }

        [TargetRpc]
        private void TargetDenied(NetworkConnection target, PickupDenial denial) =>
            _hud.Show(PickupDenialText.Describe(denial));

        private void ReleaseIfHeldBy(uint holder)
        {
            if (_holder != holder) return;

            _state.TryRelease(holder);
            _holder = NO_HOLDER;
        }

        private void OnHolderChanged(uint previous, uint current)
        {
            /// previous нужен, чтобы освободить руки прежнему держателю.
            PlayerOf(previous)?.SetHeldItem(null);
            Apply(current);

            /// Клиентам разрыв объявит RpcTeleport изнутри этого же вызова — он уйдёт
            /// раньше, чем пакет с самим SyncVar, потому что тот отправляется только
            /// в конце кадра.
            if (isServer)
            {
                _transformSync.ServerTeleport(transform.position, transform.rotation);
            }
        }

        private void Apply(uint holder)
        {
            var held = holder != NO_HOLDER;

            /// Кинематику ставим до смены родителя: иначе физический шаг успевает
            /// подхватить предмет уже в руке и утащить его вниз.
            _body.isKinematic = held || !isServer;

            /// Предмет в руке торчит перед капсулой: с включёнными столкновениями
            /// он упирался бы в стены раньше самого держателя.
            _body.detectCollisions = !held;

            var player = PlayerOf(holder);

            if (!held || player == null)
            {
                transform.SetParent(null, worldPositionStays: true);
                return;
            }

            player.SetHeldItem(this);
            transform.SetParent(player.HoldAnchor, worldPositionStays: false);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private static MirrorPlayer PlayerOf(NetworkConnectionToClient connection) =>
            connection?.identity == null ? null : connection.identity.GetComponent<MirrorPlayer>();

        /// На клиенте объекты лежат в NetworkClient.spawned, на сервере — в NetworkServer.spawned;
        /// у хоста это один и тот же объект в обоих словарях.
        private static MirrorPlayer PlayerOf(uint netId)
        {
            if (netId == NO_HOLDER) return null;

            if (NetworkClient.active && NetworkClient.spawned.TryGetValue(netId, out var onClient) && onClient != null)
            {
                return onClient.GetComponent<MirrorPlayer>();
            }

            if (NetworkServer.active && NetworkServer.spawned.TryGetValue(netId, out var onServer) && onServer != null)
            {
                return onServer.GetComponent<MirrorPlayer>();
            }

            return null;
        }
    }
}
