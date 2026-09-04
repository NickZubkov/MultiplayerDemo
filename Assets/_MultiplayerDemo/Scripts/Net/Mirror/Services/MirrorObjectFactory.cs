using System.Collections.Generic;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Net.Mirror
{
    /// В Mirror точек подключения контейнера две, и это устройство стека, а не наш выбор
    /// (Docs/Нулевой день.md § 5): на сервере объект создаём сами и отдаём готовым в
    /// NetworkServer.Spawn, реплику на удалённом клиенте создаёт делегат из RegisterPrefab.
    /// На хосте до делегата дело не доходит вовсе — FindOrSpawnObject находит серверный
    /// экземпляр в spawned и выходит раньше. Обе стороны обязаны звать одну фабрику,
    /// иначе объект соберётся по-разному.
    public sealed class MirrorObjectFactory
    {
        private readonly IObjectResolver _resolver;
        private readonly IReadOnlyList<GameObject> _prefabs;

        public MirrorObjectFactory(IObjectResolver resolver, IReadOnlyList<GameObject> prefabs)
        {
            _resolver = resolver;
            _prefabs = prefabs;
        }

        public GameObject Create(GameObject prefab, Vector3 position, Quaternion rotation) =>
            _resolver.Instantiate(prefab, position, rotation);

        /// Зовёт мост из OnStartClient, то есть уже после RegisterClientMessages, где
        /// менеджер сам кладёт префабы в NetworkClient.prefabs. Свой обработчик поверх
        /// такой записи Mirror не принимает — ругается «assetId is already used by
        /// prefab», потому что SpawnPrefab смотрит список префабов раньше обработчиков.
        /// Поэтому запись сначала снимаем.
        public void RegisterHandlers()
        {
            foreach (var prefab in _prefabs)
            {
                var owned = prefab;
                NetworkClient.UnregisterPrefab(owned);
                NetworkClient.RegisterPrefab(owned, msg => Create(owned, msg.position, msg.rotation), Release);
            }
        }

        public void UnregisterHandlers()
        {
            foreach (var prefab in _prefabs)
            {
                NetworkClient.UnregisterPrefab(prefab);
            }
        }

        private void Release(GameObject spawned) => Object.Destroy(spawned);
    }
}
