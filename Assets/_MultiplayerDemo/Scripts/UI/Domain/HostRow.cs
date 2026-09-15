using Game.Net;

namespace Game.UI
{
    /// Строка списка хостов: запись как есть плюс готовая подпись. Подпись собирает
    /// презентер — вид только рисует.
    public readonly struct HostRow
    {
        public readonly HostEntry Entry;
        public readonly string Label;

        public HostRow(HostEntry entry, string label)
        {
            Entry = entry;
            Label = label;
        }
    }
}
