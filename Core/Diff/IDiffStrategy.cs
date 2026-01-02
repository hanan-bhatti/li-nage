using System.Collections.Generic;

namespace Linage.Core.Diff
{
    public enum OperationType
    {
        Equal,
        Insert,
        Delete,
        Modify
    }

    public struct Opcode
    {
        public OperationType Type;
        public int OldStart;
        public int OldEnd;
        public int NewStart;
        public int NewEnd;
    }

    public interface IDiffStrategy
    {
        List<Opcode> ComputeDiff(string[] oldLines, string[] newLines);
        List<Opcode> GetOpcodes();
    }
}
