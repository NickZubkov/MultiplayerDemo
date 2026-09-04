using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Core
{
    /// Загрузка арены за интерфейсом по одной причине: без шва презентер лобби
    /// не проверить в EditMode — SceneManager вне плеера сцену не грузит, а порядок
    /// «сначала пол, потом сессия» — ровно то, что здесь важно не сломать.
    ///
    /// Точки спавна наружу не возвращаются: их регистрирует ArenaScope сцены и сам же
    /// отдаёт спавнеру стека. Загрузчику остаётся сцена и ничего больше.
    public interface IArenaLoader
    {
        public UniTask LoadAsync(ArenaDefinition arena, CancellationToken token);
        public UniTask UnloadAsync();
    }
}
