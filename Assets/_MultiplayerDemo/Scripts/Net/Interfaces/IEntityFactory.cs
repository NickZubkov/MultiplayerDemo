using UnityEngine;

namespace Game.Net
{
    /// Создаёт контейнером арены и кладёт в сцену арены — поэтому сетевые объекты видят
    /// сервисы арены (А-4).
    public interface IEntityFactory
    {
        public GameObject Create(GameObject prefab, Vector3 position, Quaternion rotation);
    }
}
