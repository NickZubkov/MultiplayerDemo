using System;
using System.Collections.Generic;

namespace Game.Net
{
    /// Сценовые сущности по постоянному идентификатору. Пустой идентификатор и дубликат —
    /// сломанная сцена, а не норма: падаем с именами объектов.
    public static class SceneEntityIndex
    {
        public static IReadOnlyDictionary<NetEntityId, NetEntity> Build(IEnumerable<NetEntity> entities)
        {
            var index = new Dictionary<NetEntityId, NetEntity>();

            foreach (var entity in entities)
            {
                if (string.IsNullOrEmpty(entity.SceneId))
                {
                    throw new InvalidOperationException($"У сценовой сущности {entity.name} нет идентификатора — пересохранить сцену");
                }

                var id = entity.SceneEntityId;

                if (index.TryGetValue(id, out var existing))
                {
                    throw new InvalidOperationException(
                        $"Одинаковый идентификатор у {existing.name} и {entity.name} — у копии выбрать «Новый идентификатор»");
                }

                index[id] = entity;
            }

            return index;
        }
    }
}
