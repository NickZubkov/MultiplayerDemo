namespace Game.UI
{
    /// Base — одновременно открыт один экран слоя; Overlay — поверх, не мешает;
    /// Modal — поверх всего, стопкой (спека § 8.2).
    public enum ScreenLayer
    {
        Base,
        Overlay,
        Modal,
    }
}
