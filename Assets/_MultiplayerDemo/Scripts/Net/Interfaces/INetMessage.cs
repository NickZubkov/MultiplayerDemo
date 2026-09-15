namespace Game.Net
{
    /// Сообщения — структуры с явной записью и чтением. Read меняет саму структуру:
    /// получатель заводит default и читает в неё.
    public interface INetMessage
    {
        public void Write(ref NetWriter writer);
        public void Read(ref NetReader reader);
    }
}
