using System.Collections.Generic;
using Game.Net;

namespace Game.Core
{
    /// Кто что держит — индекс, построенный из состояний ящиков на каждой машине. Руки и луч
    /// читают отсюда, поэтому HandsBusy как сеттер на компоненте ввода уходит: копии чужого
    /// состояния, которую кто-то обязан обновить, больше нет (А-5).
    public sealed class HoldRegistry
    {
        private readonly Dictionary<PlayerId, INetEntity> _heldBy = new();

        public bool IsHolding(PlayerId player) => _heldBy.ContainsKey(player);

        public bool TryGetHeldBy(PlayerId player, out INetEntity item) => _heldBy.TryGetValue(player, out item);

        public void Change(INetEntity item, PlayerId previous, PlayerId current)
        {
            if (!previous.IsNone && _heldBy.TryGetValue(previous, out var held) && held == item)
            {
                _heldBy.Remove(previous);
            }

            if (!current.IsNone) _heldBy[current] = item;
        }
    }
}
