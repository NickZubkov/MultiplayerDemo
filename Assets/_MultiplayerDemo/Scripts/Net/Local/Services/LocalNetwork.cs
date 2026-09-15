using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Net.Local
{
    /// Несколько «машин» в одном процессе (спека § 9.1). Всё, что пошло бы по проводу,
    /// кладётся в очередь и доставляется по Pump: порядок детерминирован, и тест сам решает,
    /// чья команда пришла первой. Судья — первая машина; уходит судья — судьёй становится
    /// следующая, как у мастер-клиента Fusion.
    public sealed class LocalNetwork
    {
        private readonly List<LocalMachine> _machines = new();
        private readonly List<LocalCarrier> _carriers = new();
        private readonly Dictionary<NetEntityId, NetBlob> _states = new();
        private readonly Queue<Action> _deliveries = new();

        private int _nextPlayer;

        public LocalMachine Judge { get; private set; }
        public IReadOnlyList<LocalMachine> Machines => _machines;

        public LocalMachine Join()
        {
            var machine = new LocalMachine(this, new PlayerId(_nextPlayer++));
            _machines.Add(machine);
            Judge ??= machine;
            return machine;
        }

        public void Leave(LocalMachine machine)
        {
            _machines.Remove(machine);
            _carriers.RemoveAll(carrier => carrier.Machine == machine);

            if (Judge == machine) SetJudge(_machines.FirstOrDefault());
        }

        public void SetJudge(LocalMachine judge)
        {
            Judge = judge;

            foreach (var carrier in _carriers.Where(carrier => carrier.Owner.IsNone).ToArray())
            {
                carrier.Channel.RaiseAuthorityChanged();
            }
        }

        public void Pump()
        {
            while (_deliveries.Count > 0)
            {
                _deliveries.Dequeue()();
            }
        }

        internal NetEntityChannel Create(LocalMachine machine, NetEntityId id, PlayerId owner)
        {
            var carrier = new LocalCarrier(this, machine, id, owner);
            var channel = new NetEntityChannel(carrier);
            carrier.Attach(channel);
            _carriers.Add(carrier);

            if (_states.TryGetValue(id, out var state)) _deliveries.Enqueue(() => channel.ReceiveState(state));

            return channel;
        }

        internal bool IsAuthority(LocalMachine machine, PlayerId owner) =>
            owner.IsNone ? machine == Judge : machine.Player == owner;

        internal void Publish(LocalCarrier from, NetBlob state)
        {
            _states[from.Id] = state;
            Deliver(from.Id, carrier => carrier != from, carrier => carrier.Channel.ReceiveState(state));
        }

        /// Авторитет ищется в момент доставки, а не отправки: между ними он мог смениться.
        internal void ToAuthority(LocalCarrier from, uint type, NetBlob payload)
        {
            var sender = from.Machine.Player;
            var owner = from.Owner;
            Deliver(from.Id, carrier => IsAuthority(carrier.Machine, owner),
                carrier => carrier.Channel.Receive(sender, type, payload));
        }

        internal void ToPlayer(LocalCarrier from, PlayerId target, uint type, NetBlob payload)
        {
            var sender = from.Machine.Player;
            Deliver(from.Id, carrier => carrier.Machine.Player == target,
                carrier => carrier.Channel.Receive(sender, type, payload));
        }

        internal void ToAll(LocalCarrier from, uint type, NetBlob payload)
        {
            var sender = from.Machine.Player;
            Deliver(from.Id, _ => true, carrier => carrier.Channel.Receive(sender, type, payload));
        }

        private void Deliver(NetEntityId id, Func<LocalCarrier, bool> filter, Action<LocalCarrier> delivery)
        {
            _deliveries.Enqueue(() =>
            {
                foreach (var carrier in _carriers.Where(carrier => carrier.Id == id && filter(carrier)).ToArray())
                {
                    delivery(carrier);
                }
            });
        }
    }
}
