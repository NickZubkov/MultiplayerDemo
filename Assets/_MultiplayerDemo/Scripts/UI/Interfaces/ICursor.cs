namespace Game.UI
{
    /// Курсор за швом: иначе UiService не проверить в EditMode — настоящий Cursor
    /// тянет за собой окно плеера.
    public interface ICursor
    {
        public void SetFree(bool free);
    }
}
