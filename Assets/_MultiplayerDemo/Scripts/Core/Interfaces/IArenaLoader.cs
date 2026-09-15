using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Net;

namespace Game.Core
{
    /// Загрузка арены за интерфейсом по одной причине: без шва сценарий матча не проверить
    /// в EditMode — SceneManager вне плеера сцену не грузит, а порядок «сначала пол, потом
    /// сессия» — ровно то, что здесь важно не сломать.
    ///
    /// Загрузчик возвращает мир — точки, сценовые сущности, фабрику, — MatchFlow отдаёт его
    /// сессии; порядок «точки до сессии» становится сигнатурой (И-2).
    public interface IArenaLoader
    {
        public UniTask<INetWorld> LoadAsync(ArenaDefinition arena, CancellationToken token);
        public UniTask UnloadAsync();
    }
}
