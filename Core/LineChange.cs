namespace Linage.Core
{
    public class LineChange
    {
        public int LineNumber { get; set; }
        public string OldHash { get; set; } = string.Empty;
        public string NewHash { get; set; } = string.Empty;
    }
}
