using System;
using Unity.Netcode;
using UnityEngine;
using VContainer.Unity;

namespace Game.Net.Ngo
{
    /// Точки спавна NGO (спека § 5.10): обработчики создания префабов, место аватара в
    /// подтверждении подключения, мир — судьёй после старта. Любой сетевой объект стека создаёт
    /// фабрика мира: сущности видят сервисы арены (А-4), носитель сразу получает мир.
    public sealed class NgoSpawner : IStartable, IDisposable
    {
        private const string NOT_IN_MATCH = "Хост не в матче";

        private readonly NetworkManager _manager;
        private readonly EntityCatalog _catalog;
        private readonly GameObject _sceneCarrier;

        private INetWorld _world;

        public NgoSpawner(NetworkManager manager, EntityCatalog catalog, GameObject sceneCarrier)
        {
            _manager = manager;
            _catalog = catalog;
            _sceneCarrier = sceneCarrier;
        }

        /// Обработчики — один раз на жизнь scope и до любой сессии: сообщение о спавне приходит
        /// в первых же пакетах после подключения.
        public void Start()
        {
            _manager.ConnectionApprovalCallback = Approve;

            foreach (var prefab in _catalog.Prefabs)
            {
                _manager.PrefabHandler.AddHandler(prefab, new Handler(this, prefab));
            }

            _manager.PrefabHandler.AddHandler(_sceneCarrier, new Handler(this, _sceneCarrier));
        }

        public void Dispose()
        {
            if (_manager == null) return;

            _manager.ConnectionApprovalCallback = null;

            foreach (var prefab in _catalog.Prefabs)
            {
                _manager.PrefabHandler.RemoveHandler(prefab);
            }

            _manager.PrefabHandler.RemoveHandler(_sceneCarrier);
        }

        /// Сессия отдаёт мир до StartHost и StartClient: место аватара NGO спрашивает уже внутри
        /// StartHost, а у клиента объекты появляются раньше его ClientConnected (спайк, вопрос 5).
        public void Use(INetWorld world) => _world = world;

        public void Forget() => _world = null;

        /// Судья после старта: носители сценовых сущностей, затем динамические по размещениям.
        public void PopulateWorld()
        {
            foreach (var entity in _world.SceneEntities)
            {
                var at = entity.transform;
                Spawn(_sceneCarrier, at.position, at.rotation, entity.SceneEntityId);
            }

            foreach (var placement in _world.Placements)
            {
                Spawn(Prefab(placement.EntityType), placement.Position, placement.Rotation, NetEntityId.NONE);
            }
        }

        private void Spawn(GameObject prefab, Vector3 position, Quaternion rotation, NetEntityId sceneEntity)
        {
            var instance = Create(prefab, position, rotation);
            instance.GetComponent<NgoCarrier>().MarkScene(sceneEntity);
            instance.Spawn();
        }

        private NetworkObject Create(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var instance = _world.Factory.Create(prefab, position, rotation);
            instance.GetComponent<NgoCarrier>().UseWorld(_world);
            return instance.GetComponent<NetworkObject>();
        }

        /// Тип без префаба в каталоге — сломанная сборка: говорим сразу и именем.
        private GameObject Prefab(string entityType)
        {
            if (_catalog.TryGetPrefab(entityType, out var prefab)) return prefab;

            throw new InvalidOperationException($"В каталоге NGO нет префаба для «{entityType}»");
        }

        /// Подтверждение включено ради места аватара. Поза — из политики мира по номеру игрока,
        /// одинаково на любой машине (И-19), а не по счётчику хоста.
        private void Approve(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            if (_world == null)
            {
                response.Approved = false;
                response.Reason = NOT_IN_MATCH;
                return;
            }

            var pose = _world.AvatarPose(NgoIds.Player(request.ClientNetworkId));

            response.Approved = true;
            response.CreatePlayerObject = true;
            response.Position = pose.position;
            response.Rotation = pose.rotation;
        }

        /// Объект, приехавший по сети, и аватар по подтверждению NGO создаёт этим обработчиком —
        /// тем же путём, что судья создаёт мир.
        private sealed class Handler : INetworkPrefabInstanceHandler
        {
            private readonly NgoSpawner _spawner;
            private readonly GameObject _prefab;

            public Handler(NgoSpawner spawner, GameObject prefab)
            {
                _spawner = spawner;
                _prefab = prefab;
            }

            public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation) =>
                _spawner.Create(_prefab, position, rotation);

            public void Destroy(NetworkObject networkObject) => UnityEngine.Object.Destroy(networkObject.gameObject);
        }
    }
}
