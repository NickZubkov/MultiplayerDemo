using System;
using R3;
using UnityEngine;

namespace Game.Net
{
    /// Метка «этот объект сетевой» (спека § 5.5). Арена знает, что объект сетевой, но не
    /// знает, через какой стек: сюда стек привязывает свой носитель, игра подписывается на Bound.
    ///
    /// Сценовой сущности нужен постоянный идентификатор — одинаковый на всех машинах, потому
    /// что сцену каждая грузит сама. Он генерируется при первом появлении объекта в
    /// сохранённой сцене; в префабе он пуст, иначе все экземпляры получили бы один.
    [DisallowMultipleComponent]
    public sealed class NetEntity : MonoBehaviour
    {
        [SerializeField] private NetEntityKind _kind = NetEntityKind.Dynamic;
        [SerializeField] private string _entityType;
        [SerializeField] private string _sceneId;

        private readonly ReactiveProperty<INetEntity> _bound = new();

        public NetEntityKind Kind => _kind;
        public string EntityType => _entityType;
        public string SceneId => _sceneId;
        public NetEntityId SceneEntityId => NetEntityId.Scene(_sceneId);
        public ReadOnlyReactiveProperty<INetEntity> Bound => _bound;

        private void OnDestroy() => _bound.Dispose();

        /// Разрушенная метка молчит, а не бросает. Носитель снимает привязку в конце жизни
        /// объекта, но порядок разрушения компонентов Unity не обещает: у NGO OnNetworkDespawn
        /// приходит из OnDestroy сетевого объекта — то есть уже после того, как эта метка
        /// похоронила своё свойство. Слушателей у неё к этому моменту всё равно нет.
        public void Bind(INetEntity entity)
        {
            if (_bound.IsDisposed) return;

            _bound.Value = entity;
        }

        /// Путь сцены проверяется, а не только IsValid: в режиме префаба объект тоже живёт
        /// в валидной, но временной сцене, и идентификатор уехал бы в сам префаб.
        private void OnValidate()
        {
            if (_kind != NetEntityKind.Scene || !string.IsNullOrEmpty(_sceneId)) return;
            if (!gameObject.scene.IsValid() || !gameObject.scene.path.EndsWith(".unity")) return;

            _sceneId = Guid.NewGuid().ToString("N");
        }

        /// Копия объекта через Ctrl+D уносит и идентификатор — сбор арены на такое падает
        /// с именами обоих объектов, лечится этим пунктом меню.
        [ContextMenu("Новый идентификатор")]
        private void RegenerateSceneId() => _sceneId = Guid.NewGuid().ToString("N");
    }
}
