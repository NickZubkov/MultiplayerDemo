using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Net
{
    /// Тип сущности → префаб. У каждого стека свой ассет: у Local — базовые префабы, у сетевых
    /// стеков — их сетевые варианты с носителем и синхронизацией позы (Ц13).
    [CreateAssetMenu(menuName = "Демка/Каталог сущностей", fileName = "Entities_")]
    public sealed class EntityCatalog : ScriptableObject
    {
        /// Тип сущности аватара. Константа сети, а не игры: «у каждого игрока ровно один аватар»
        /// — понятие всех четырёх стеков (объект игрока у NGO и Mirror, SetPlayerObject у Fusion),
        /// и лежит она там, куда дотягиваются все четыре, — в Game.Net.
        public const string AVATAR_TYPE = "player";

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        /// Все префабы каталога: NGO и Mirror регистрируют обработчики создания заранее,
        /// до первого сообщения о спавне.
        public IEnumerable<GameObject> Prefabs
        {
            get
            {
                foreach (var entry in _entries)
                {
                    yield return entry.Prefab;
                }
            }
        }

        public bool TryGetPrefab(string entityType, out GameObject prefab)
        {
            foreach (var entry in _entries)
            {
                if (entry.EntityType != entityType) continue;

                prefab = entry.Prefab;
                return true;
            }

            prefab = null;
            return false;
        }

        [Serializable]
        private struct Entry
        {
            [SerializeField] private string _entityType;
            [SerializeField] private GameObject _prefab;

            public string EntityType => _entityType;
            public GameObject Prefab => _prefab;
        }
    }
}
