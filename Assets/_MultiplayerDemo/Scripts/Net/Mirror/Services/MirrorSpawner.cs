using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Game.Net.Mirror
{
    /// Точки спавна Mirror (спека § 5.10). Их две, и это устройство стека (Нулевой день § 5):
    /// сервер создаёт объект сам и отдаёт готовым в NetworkServer.Spawn, реплику у клиента
    /// создаёт делегат из RegisterPrefab. У хоста до делегата не доходит — он находит серверный
    /// экземпляр. Обе стороны зовут одну фабрику мира.
    public sealed class MirrorSpawner
    {
        private readonly EntityCatalog _catalog;
        private readonly GameObject _sceneCarrier;
        private readonly MirrorPlayers _players;

        private INetWorld _world;

        public MirrorSpawner(EntityCatalog catalog, GameObject sceneCarrier, MirrorPlayers players)
        {
            _catalog = catalog;
            _sceneCarrier = sceneCarrier;
            _players = players;
        }

        /// Сессия отдаёт мир до StartHost и StartClient: аватар сервер создаёт сразу после
        /// подключения, а реплики у клиента — по первым сообщениям о спавне.
        public void Use(INetWorld world) => _world = world;

        public void Forget() => _world = null;

        /// Зовёт мост из OnStartClient — после RegisterClientMessages, где менеджер сам кладёт
        /// префабы в NetworkClient.prefabs. Поверх такой записи свой делегат Mirror не принимает
        /// («assetId is already used by prefab»), поэтому запись сначала снимаем.
        public void RegisterHandlers()
        {
            foreach (var prefab in Prefabs())
            {
                var owned = prefab;
                NetworkClient.UnregisterPrefab(owned);
                NetworkClient.RegisterPrefab(owned, message => Create(owned, message.position, message.rotation), Release);
            }
        }

        public void UnregisterHandlers()
        {
            foreach (var prefab in Prefabs())
            {
                NetworkClient.UnregisterPrefab(prefab);
            }
        }

        /// Зовёт мост из OnServerAddPlayer. Поза — из политики мира по номеру игрока (И-19),
        /// а не из NetworkManager.startPositions, о которых загрузчик арены не знает.
        public void SpawnAvatar(NetworkConnectionToClient connection)
        {
            var player = _players.PlayerOf(connection);
            var pose = _world.AvatarPose(player);
            var avatar = Create(Prefab(EntityCatalog.AVATAR_TYPE), pose.position, pose.rotation);

            avatar.GetComponent<MirrorCarrier>().Mark(NetEntityId.NONE, player);
            avatar.name = $"{avatar.name} [игрок {player}]";
            NetworkServer.AddPlayerForConnection(connection, avatar);
        }

        /// Судья после старта: носители сценовых сущностей, затем динамические по размещениям.
        /// Во владение клиентам не передаются: уходящему Mirror уничтожает всё, чем он владел.
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
            instance.GetComponent<MirrorCarrier>().Mark(sceneEntity, PlayerId.NONE);
            NetworkServer.Spawn(instance);
        }

        private GameObject Create(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var instance = _world.Factory.Create(prefab, position, rotation);
            instance.GetComponent<MirrorCarrier>().UseWorld(_world, _players);
            return instance;
        }

        private IEnumerable<GameObject> Prefabs()
        {
            foreach (var prefab in _catalog.Prefabs)
            {
                yield return prefab;
            }

            yield return _sceneCarrier;
        }

        /// Тип без префаба в каталоге — сломанная сборка: говорим сразу и именем.
        private GameObject Prefab(string entityType)
        {
            if (_catalog.TryGetPrefab(entityType, out var prefab)) return prefab;

            throw new InvalidOperationException($"В каталоге Mirror нет префаба для «{entityType}»");
        }

        private static void Release(GameObject spawned) => UnityEngine.Object.Destroy(spawned);
    }
}
