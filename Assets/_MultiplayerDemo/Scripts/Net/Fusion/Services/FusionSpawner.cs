using System;
using Fusion;
using UnityEngine;

namespace Game.Net.Fusion
{
    /// Точки спавна Fusion (спека § 5.10). Сервера нет, обязанности разделены: мир создаёт
    /// мастер-клиент после старта, аватар каждый создаёт себе сам. Экземпляры — и свои, и
    /// чужие — создаёт провайдер объектов раннера, а он зовёт сюда.
    public sealed class FusionSpawner
    {
        private readonly EntityCatalog _catalog;
        private readonly GameObject _sceneCarrier;

        private INetWorld _world;

        public FusionSpawner(EntityCatalog catalog, GameObject sceneCarrier)
        {
            _catalog = catalog;
            _sceneCarrier = sceneCarrier;
        }

        /// Сессия отдаёт мир до StartGame: свой аватар создаётся из OnPlayerJoined, который
        /// приходит ещё внутри старта.
        public void Use(INetWorld world) => _world = world;

        public void Forget() => _world = null;

        /// Позу Fusion ставит после создания сам — из аргументов Spawn или из состояния
        /// NetworkTransform, поэтому фабрике достаётся нулевая.
        public NetworkObject Instantiate(NetworkObject prefab)
        {
            var instance = _world.Factory.Create(prefab.gameObject, Vector3.zero, Quaternion.identity);
            instance.GetComponent<FusionCarrier>().UseWorld(_world);
            return instance.GetComponent<NetworkObject>();
        }

        /// Мост из OnPlayerJoined: свой аватар — только на входе самого себя; чужие сделают то
        /// же у себя, и власть над аватаром останется у владельца.
        public void SpawnAvatar(NetworkRunner runner, PlayerRef player)
        {
            if (player != runner.LocalPlayer) return;

            var pose = _world.AvatarPose(FusionIds.Player(player));
            var avatar = runner.Spawn(Prefab(EntityCatalog.AVATAR_TYPE), pose.position, pose.rotation, player);

            /// Связь «игрок → аватар» Fusion раздаёт всем, а объявить её может только владелец.
            runner.SetPlayerObject(player, avatar);
        }

        /// Мастер-клиент после StartGame. Власть над этими объектами у него по флагу
        /// MasterClientObject и перейдёт к новому мастеру при его уходе (спайк, вопрос 2).
        public void PopulateWorld(NetworkRunner runner)
        {
            if (!runner.IsSharedModeMasterClient) return;

            foreach (var entity in _world.SceneEntities)
            {
                var id = entity.SceneEntityId.Value;
                var at = entity.transform;
                runner.Spawn(_sceneCarrier, at.position, at.rotation, null,
                    (_, spawned) => spawned.GetComponent<FusionCarrier>().SceneEntity = id);
            }

            foreach (var placement in _world.Placements)
            {
                runner.Spawn(Prefab(placement.EntityType), placement.Position, placement.Rotation);
            }
        }

        /// Тип без префаба в каталоге — сломанная сборка: говорим сразу и именем.
        private GameObject Prefab(string entityType)
        {
            if (_catalog.TryGetPrefab(entityType, out var prefab)) return prefab;

            throw new InvalidOperationException($"В каталоге Fusion нет префаба для «{entityType}»");
        }
    }
}
