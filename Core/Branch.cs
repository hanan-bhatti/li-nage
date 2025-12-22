namespace Linage.Core
{
    public class Branch
    {
        public string Name { get; set; } = string.Empty;
        public Commit Head { get; set; }
    }
}
