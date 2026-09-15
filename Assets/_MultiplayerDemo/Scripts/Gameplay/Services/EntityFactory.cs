using Game.Net;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay
{
    /// Создаёт сущности контейнером арены и кладёт в сцену арены: сетевые объекты видят
    /// сервисы матча (А-4) и уходят вместе с ареной, а не остаются в Bootstrap (S12).
    public sealed class EntityFactory : IEntityFactory
    {
        private readonly IObjectResolver _resolver;
        private readonly Scene _scene;

        public EntityFactory(IObjectResolver resolver, LifetimeScope scope)
        {
            _resolver = resolver;
            _scene = scope.gameObject.scene;
        }

        public GameObject Create(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var instance = _resolver.Instantiate(prefab, position, rotation);
            SceneManager.MoveGameObjectToScene(instance, _scene);
            return instance;
        }
    }
}
