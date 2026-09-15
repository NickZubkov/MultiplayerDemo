using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Game.Net.Local
{
    /// Одна машина, которая сама себе хост: сущности мира и свой аватар она создаёт сама,
    /// а «по проводу» ходит через LocalNetwork — тот же путь, что у трёх сетевых стеков.
    public sealed class LocalSession : INetSession, IDisposable
    {
        private const string NOBODY_TO_JOIN = "Без сети подключаться не к кому";

        private static readonly PlayerId ME = new(0);

        private readonly SessionStatePublisher _publisher;
        private readonly PlayerId[] _players = { ME };
        private readonly FrameProvider _frames;
        private readonly EntityCatalog _catalog;
        private readonly List<NetEntityChannel> _channels = new();

        private LocalNetwork _network;
        private IDisposable _pump;
        private ulong _nextDynamicId;

        public ReadOnlyReactiveProperty<SessionState> State => _publisher.State;
        public PlayerId LocalPlayer => ME;
        public bool IsJudge => true;
        public IReadOnlyCollection<PlayerId> Players => _players;
        public Observable<PlayerId> PlayerJoined => Observable.Empty<PlayerId>();
        public Observable<PlayerId> PlayerLeft => Observable.Empty<PlayerId>();

        public LocalSession(FrameProvider frames, EntityCatalog catalog)
        {
            _publisher = new SessionStatePublisher(frames);
            _frames = frames;
            _catalog = catalog;
        }

        public void Dispose()
        {
            _pump?.Dispose();
            _publisher.Dispose();
        }

        /// Контракт сессии: не бросать. Тип без префаба в каталоге станет Failed с тем же
        /// текстом и уйдёт игроку в HUD.
        public UniTask StartHostAsync(SessionSettings settings, INetWorld world, CancellationToken token)
        {
            _publisher.Begin(SessionPhase.Hosting);
            return _publisher.GuardAsync(() => Populate(world));
        }

        public UniTask JoinAsync(HostEntry entry, INetWorld world, CancellationToken token)
        {
            _publisher.Begin(SessionPhase.Connecting);
            _publisher.Report(new SessionState(SessionPhase.Failed, NOBODY_TO_JOIN));
            return UniTask.CompletedTask;
        }

        public UniTask LeaveAsync()
        {
            _pump?.Dispose();
            _pump = null;

            foreach (var channel in _channels)
            {
                channel.Dispose();
            }

            _channels.Clear();
            _network = null;
            _publisher.End();
            return UniTask.CompletedTask;
        }

        /// Одна машина: судья она же. Порядок как у сетевых стеков — сначала сценовые сущности
        /// и размещения мира (их создаёт судья), потом свой аватар.
        private UniTask Populate(INetWorld world)
        {
            _network = new LocalNetwork();
            var machine = _network.Join();

            foreach (var entity in world.SceneEntities)
            {
                entity.Bind(Track(machine.CreateEntity(entity.SceneEntityId, PlayerId.NONE)));
            }

            foreach (var placement in world.Placements)
            {
                Spawn(world, machine, placement.EntityType, placement.Position, placement.Rotation, PlayerId.NONE);
            }

            var pose = world.AvatarPose(ME);
            Spawn(world, machine, EntityCatalog.AVATAR_TYPE, pose.position, pose.rotation, ME);

            _pump = Observable.EveryUpdate(_frames).Subscribe(_ => _network.Pump());
            return UniTask.CompletedTask;
        }

        /// Тип без префаба в каталоге — сломанная сборка: говорим сразу и именем.
        private void Spawn(INetWorld world, LocalMachine machine, string entityType, Vector3 position,
            Quaternion rotation, PlayerId owner)
        {
            if (!_catalog.TryGetPrefab(entityType, out var prefab))
            {
                throw new InvalidOperationException($"В каталоге стека нет префаба для «{entityType}»");
            }

            var instance = world.Factory.Create(prefab, position, rotation);
            var id = NetEntityId.Dynamic(++_nextDynamicId);
            instance.GetComponent<NetEntity>().Bind(Track(machine.CreateEntity(id, owner)));
        }

        private NetEntityChannel Track(NetEntityChannel channel)
        {
            _channels.Add(channel);
            return channel;
        }
    }
}
