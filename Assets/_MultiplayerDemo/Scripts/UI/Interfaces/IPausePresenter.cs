using R3;

namespace Game.UI
{
    /// Узкий интерфейс презентера (спека § 8.3): ровно то, что нужно снаружи UI.
    public interface IPausePresenter
    {
        public ReadOnlyReactiveProperty<bool> IsOpen { get; }

        public void Open();
        public void Close();
    }
}
