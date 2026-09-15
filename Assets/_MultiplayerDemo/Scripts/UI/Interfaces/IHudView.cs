namespace Game.UI
{
    /// HUD — такой же экран, как остальные: его показывает и прячет UiService. Сообщения
    /// приходят отдельным вызовом, и гасит их сам вид по таймеру.
    public interface IHudView
    {
        public void Show();
        public void Hide();
        public void ShowMessage(string text);
    }
}
