namespace Linage.Core
{
    public class LineTracker
    {
        public string Strategy { get; set; } = "DiffMatchPatch";

        public LineChange ProduceChange() 
        { 
            return new LineChange(); 
        }
    }
}
