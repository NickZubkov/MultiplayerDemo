using Game.Core;
using R3;

namespace Game.UI
{
    public interface ILobbyPresenter
    {
        public ReadOnlyReactiveProperty<ArenaDefinition> SelectedArena { get; }
    }
}
