using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Core
{
    /// Загрузка арены за интерфейсом по одной причине: без шва презентер лобби
    /// не проверить в EditMode — SceneManager вне плеера сцену не грузит, а порядок
    /// «сначала пол, потом сессия» — ровно то, что здесь важно не сломать.
    ///
    /// Возвращает точки спавна, а не голый UniTask: искать их по сцене — дело того,
    /// кто её загрузил, и незачем тащить Game.Gameplay в презентер и в тесты.
    public interface IArenaLoader
    {
        public UniTask<ISpawnPointRegistry> LoadAsync(CancellationToken token);
        public UniTask UnloadAsync();
    }
}
