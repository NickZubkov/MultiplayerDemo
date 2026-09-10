using Fusion;
using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.Net.Fusion
{
    /// Здесь модель расходится с двумя другими стеками сильнее всего. Сервера нет, судьи
    /// нет, и автомат ItemState из Game.Core на этой стороне не работает: право изменять
    /// предмет — это власть над состоянием, и выдаёт её сам Fusion. Гонку он же и разрешает —
    /// RequestStateAuthority может не выдать авторитет, и это штатный отказ, а не ошибка.
    ///
    /// Правила взятия при этом остаются общими: PickupRules спрашивает тот, кто хочет взять,
    /// потому что спросить больше некого. Проверка честная ровно настолько, насколько честен
    /// сам клиент, — и это цена модели, а не недосмотр (расхождение уходит в таблицу задачи 16).
    ///
    /// Держатель едет одним сетевым свойством, а всё остальное каждая машина делает у себя,
    /// увидев его смену: руки, кинематику, столкновения и саму позицию в руке.
    [RequireComponent(typeof(NetworkObject), typeof(Rigidbody))]
    public sealed class FusionItem : NetworkBehaviour, IStateAuthorityChanged
    {
        [Networked] public PlayerRef Holder { get; set; }

        private Rigidbody _body;
        private IHudMessages _hud;

        /// Держатель, под которого уже настроены руки и физика. Сравнение с Holder —
        /// это и есть обнаружение смены: отдельный ChangeDetector тут ничего не добавит.
        private PlayerRef _shownHolder = PlayerRef.None;

        /// Мы попросили авторитет и ждём ответа. Ответом может быть и молчание —
        /// тогда отказ виден по тому, что предмет достался другому.
        private bool _requested;

        /// Бросок, отложенный до возвращения власти над предметом.
        private Vector3? _pendingImpulse;

        [Inject]
        public void Construct(IHudMessages hud) => _hud = hud;

        private void Awake() => _body = GetComponent<Rigidbody>();

        /// Опоздавший клиент получает предмет уже занятым, и первый же Render поставит
        /// его в чужую руку. Физику надо привести в порядок раньше: у чужого предмета
        /// тело обязано быть кинематическим, иначе он успеет упасть по своей гравитации.
        public override void Spawned() => ApplyPhysics();

        public void TryTake(FusionPlayer player)
        {
            var query = new PickupQuery(
                itemFree: Holder == PlayerRef.None,
                handsEmpty: !player.HandsBusy,
                distance: Vector3.Distance(player.transform.position, transform.position),
                hasLineOfSight: true);   // луч уже проверен тем же клиентом, дистанции хватает

            var denial = PickupRules.Evaluate(query);

            if (denial != PickupDenial.None)
            {
                _hud.Show(PickupDenialText.Describe(denial));
                return;
            }

            _pendingImpulse = null;

            /// Авторитет может быть уже у нас: ящики создал мастер-клиент, и у него власть
            /// над всеми ими сразу, а после броска она остаётся у бросившего. Просить его
            /// в этом случае не у кого — Fusion ответит молча, и обратного вызова не будет.
            if (HasStateAuthority)
            {
                Holder = Runner.LocalPlayer;
                ApplyHolder();
                return;
            }

            _requested = true;
            Object.RequestStateAuthority();
        }

        public void Release(Vector3 impulse)
        {
            if (Holder != Runner.LocalPlayer) return;

            /// Власти может не быть даже у держателя: чужой запрос мог прийти уже после
            /// того, как предмет взяли мы, и Fusion всё равно отдал власть просившему.
            /// Бросок тогда ждёт её возвращения — иначе предмет застрял бы в руках.
            if (!HasStateAuthority)
            {
                _pendingImpulse = impulse;
                Object.RequestStateAuthority();
                return;
            }

            Drop(impulse);
        }

        /// Зовёт Fusion, когда власть над предметом сменилась — и у того, кто её получил,
        /// и у того, кто её потерял.
        public void StateAuthorityChanged()
        {
            ApplyPhysics();

            if (!HasStateAuthority)
            {
                _requested = false;
                return;
            }

            /// Власть вернулась к держателю, который уже успел нажать «бросить».
            if (_pendingImpulse.HasValue && Holder == Runner.LocalPlayer)
            {
                var impulse = _pendingImpulse.Value;
                _pendingImpulse = null;
                Drop(impulse);
                return;
            }

            if (!_requested) return;

            _requested = false;

            /// Пока запрос летел, предмет могли взять: очередь запросов Fusion разбирает
            /// по одному, и авторитет нам всё равно отдадут — брать просто уже нечего.
            if (Holder != PlayerRef.None)
            {
                _hud.Show(PickupDenialText.Describe(PickupDenial.ItemHeld));
                return;
            }

            Holder = Runner.LocalPlayer;
            ApplyHolder();
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || Holder == PlayerRef.None) return;

            /// Держатель вышел из игры: его аватар исчез, и предмет остался бы висеть
            /// в воздухе. Власть над объектом ушедшего Fusion отдаёт мастер-клиенту —
            /// он и роняет предмет, больше это сделать некому (пункт 7 чек-листа).
            if (!InSession(Holder))
            {
                Holder = PlayerRef.None;
                ApplyHolder();
                return;
            }

            /// Позицию в руке пишет авторитет: иначе сетевое состояние осталось бы там,
            /// где предмет подняли, и после броска он улетел бы обратно на то место.
            var hold = HoldOf(Holder);
            if (hold != null) transform.SetPositionAndRotation(hold.position, hold.rotation);
        }

        public override void Render() => ApplyHolder();

        /// Предмет в руке ведёт сама рука, а не присланная позиция: чужие капсулы Fusion
        /// рисует интерполяцией, и снятый тиком снимок предмета отставал бы от них.
        ///
        /// LateUpdate, а не Render: вся работа Fusion, включая интерполяцию, происходит
        /// внутри NetworkRunner.Update, а порядок объектов внутри неё не обещан — рука
        /// могла бы встать на место уже после того, как предмет к ней подтянулся.
        private void LateUpdate()
        {
            if (Object == null || !Object.IsValid || Holder == PlayerRef.None) return;

            var hold = HoldOf(Holder);
            if (hold != null) transform.SetPositionAndRotation(hold.position, hold.rotation);
        }

        private void Drop(Vector3 impulse)
        {
            Holder = PlayerRef.None;

            /// Руки и физику применяем здесь же, не дожидаясь Render: импульс
            /// кинематическому телу не дойдёт, а кинематику снимает именно ApplyHolder.
            ApplyHolder();
            _body.AddForce(impulse, ForceMode.Impulse);
        }

        private void ApplyHolder()
        {
            if (_requested && Holder != PlayerRef.None && Holder != Runner.LocalPlayer)
            {
                /// Предмет достался другому, и авторитета нам уже не дадут: молчание —
                /// это и есть отказ, но игроку нужна причина.
                _requested = false;
                _hud.Show(PickupDenialText.Describe(PickupDenial.ItemHeld));
            }

            if (_shownHolder == Holder) return;

            SetHands(_shownHolder, null);
            SetHands(Holder, this);
            _shownHolder = Holder;
            ApplyPhysics();
        }

        private void ApplyPhysics()
        {
            var held = Holder != PlayerRef.None;

            /// Физику считает только авторитет предмета, остальные видят присланный
            /// трансформ. В руке тело кинематическое у всех: его ведёт рука, а не силы.
            _body.isKinematic = held || !HasStateAuthority;

            /// Предмет в руке торчит перед капсулой: с включёнными столкновениями
            /// он упирался бы в стены раньше самого держателя.
            _body.detectCollisions = !held;
        }

        private void SetHands(PlayerRef player, FusionItem item)
        {
            var holder = ObjectOf(player);
            if (holder != null && holder.TryGetComponent<FusionPlayer>(out var target)) target.SetHeldItem(item);
        }

        /// Спрашиваем список игроков сессии, а не наличие аватара: аватар мог ещё не
        /// доехать до этой машины, и по нему предмет выпадал бы из рук на ровном месте.
        private bool InSession(PlayerRef player)
        {
            foreach (var active in Runner.ActivePlayers)
            {
                if (active == player) return true;
            }

            return false;
        }

        private Transform HoldOf(PlayerRef player)
        {
            var holder = ObjectOf(player);
            return holder != null && holder.TryGetComponent<FusionPlayer>(out var target) ? target.HoldAnchor : null;
        }

        /// Аватар игрока по его номеру: связь объявляет сам владелец через SetPlayerObject
        /// сразу после спавна, и Fusion раздаёт её всем.
        private NetworkObject ObjectOf(PlayerRef player)
        {
            if (player == PlayerRef.None || Runner == null) return null;

            return Runner.GetPlayerObject(player);
        }
    }
}
