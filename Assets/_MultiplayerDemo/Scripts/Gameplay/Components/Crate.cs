using Game.Core;
using Game.Net;
using R3;
using UnityEngine;
using VContainer;

namespace Game.Gameplay
{
    /// Оболочка ящика: представление плюс адаптер к физике (Ц9). Решений здесь нет — только
    /// применение того, что решила CrateLogic: ни одного if про правила игры.
    [RequireComponent(typeof(Rigidbody), typeof(NetEntity))]
    public sealed class Crate : MonoBehaviour
    {
        private Rigidbody _body;
        private NetEntity _net;
        private AvatarRegistry _avatars;
        private HoldRegistry _holds;
        private IHudMessages _hud;
        private DemoConfig _config;
        private INetEntity _entity;
        private CrateLogic _logic;
        private DisposableBag _subscriptions;

        [Inject]
        public void Construct(AvatarRegistry avatars, HoldRegistry holds, IHudMessages hud, DemoConfig config)
        {
            _avatars = avatars;
            _holds = holds;
            _hud = hud;
            _config = config;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _net = GetComponent<NetEntity>();
        }

        private void Start() => _net.Bound.Where(entity => entity != null).Subscribe(OnBound).AddTo(ref _subscriptions);

        private void OnDestroy()
        {
            _subscriptions.Dispose();
            _logic?.Dispose();
        }

        /// Позу в руке каждая машина ставит сама, а не получает по сети: смещение — константа
        /// префаба, а рука чужого аватара и так уже здесь. Репликация добавила бы задержку
        /// (цель D5 без передачи владения, спека § 5.8).
        private void LateUpdate()
        {
            if (_logic == null) return;

            var holder = _logic.Holder.CurrentValue;
            if (holder.IsNone || !_avatars.TryGet(holder, out var avatar)) return;

            var hold = avatar.HoldPoint;
            transform.SetPositionAndRotation(hold.position, hold.rotation);
        }

        private void OnBound(INetEntity entity)
        {
            _entity = entity;
            _logic = new CrateLogic(entity, _avatars, _holds, _hud, () => transform.position, _config.ThrowImpulse);
            _logic.Holder.Subscribe(_ => ApplyPhysics()).AddTo(ref _subscriptions);
            _logic.Thrown.Subscribe(Throw).AddTo(ref _subscriptions);
            entity.AuthorityChanged.Subscribe(_ => ApplyPhysics()).AddTo(ref _subscriptions);
        }

        /// Физику свободного ящика считает авторитет, остальные видят присланную позу. В руке
        /// тело кинематическое у всех и без столкновений: ящик торчит перед капсулой и упирался
        /// бы в стены раньше держателя.
        private void ApplyPhysics()
        {
            var held = !_logic.Holder.CurrentValue.IsNone;
            _body.isKinematic = held || !_entity.IsAuthority;
            _body.detectCollisions = !held;
        }

        /// Разрыв движения — до импульса: буферы интерполяции у остальных помнят позы из руки,
        /// и без него ящик уехал бы к ним после броска.
        private void Throw(Vector3 impulse)
        {
            _entity.Snap();
            _body.AddForce(impulse, ForceMode.Impulse);
        }
    }
}
