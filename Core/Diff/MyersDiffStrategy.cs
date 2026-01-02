using System;
using System.Collections.Generic;

namespace Linage.Core.Diff
{
    /// <summary>
    /// Implements the Myers O(ND) diff algorithm.
    /// Used for finding the shortest edit script (SES) between two sequences of lines.
    /// Spec: 5.3
    /// </summary>
    public class MyersDiffStrategy : IDiffStrategy
    {
        private List<Opcode> _opcodes;

        public List<Opcode> ComputeDiff(string[] oldLines, string[] newLines)
        {
            _opcodes = new List<Opcode>();
            if (oldLines == null) oldLines = new string[0];
            if (newLines == null) newLines = new string[0];

            int n = oldLines.Length;
            int m = newLines.Length;

            int max = n + m;
            int[] v = new int[2 * max + 1];
            
            // Traceback data structure
            var trace = new List<Dictionary<int, int>>();

            for (int d = 0; d <= max; d++)
            {
                var vCopy = new Dictionary<int, int>();
                // We only need to store the relevant range of k for this d
                for (int k = -d; k <= d; k += 2)
                {
                    int x;
                    if (k == -d || (k != d && v[k - 1 + max] < v[k + 1 + max]))
                    {
                        x = v[k + 1 + max];
                    }
                    else
                    {
                        x = v[k - 1 + max] + 1;
                    }

                    int y = x - k;
                    
                    // Store the starting point of the snake for traceback
                    vCopy[k] = x;

                    while (x < n && y < m && oldLines[x] == newLines[y])
                    {
                        x++;
                        y++;
                    }

                    v[k + max] = x;

                    if (x >= n && y >= m)
                    {
                        // Found solution
                        trace.Add(vCopy);
                        _opcodes = BuildOpcodes(trace, oldLines, newLines, n, m);
                        return _opcodes;
                    }
                }
                trace.Add(vCopy);
            }
            return _opcodes;
        }

        private List<Opcode> BuildOpcodes(List<Dictionary<int, int>> trace, string[] oldLines, string[] newLines, int n, int m)
        {
            var opcodes = new List<Opcode>();
            int x = n;
            int y = m;

            // Backtrack from the end
            for (int d = trace.Count - 1; d >= 0; d--)
            {
                var v = trace[d];
                int k = x - y;
                
                int prevK;
                if (k == -d || (k != d && GetV(v, k - 1) < GetV(v, k + 1)))
                {
                    prevK = k + 1;
                }
                else
                {
                    prevK = k - 1;
                }

                int prevX = GetV(v, prevK);
                int prevY = prevX - prevK;

                while (x > prevX && y > prevY)
                {
                    // Diagonal move (Equal)
                    opcodes.Add(new Opcode
                    {
                        Type = OperationType.Equal,
                        OldStart = x - 1,
                        OldEnd = x,
                        NewStart = y - 1,
                        NewEnd = y
                    });
                    x--;
                    y--;
                }

                if (d > 0)
                {
                    if (x == prevX)
                    {
                        // Vertical move (Insert in new)
                        opcodes.Add(new Opcode
                        {
                            Type = OperationType.Insert,
                            OldStart = x,
                            OldEnd = x,
                            NewStart = y - 1,
                            NewEnd = y
                        });
                        y--;
                    }
                    else if (y == prevY)
                    {
                        // Horizontal move (Delete from old)
                        opcodes.Add(new Opcode
                        {
                            Type = OperationType.Delete,
                            OldStart = x - 1,
                            OldEnd = x,
                            NewStart = y,
                            NewEnd = y
                        });
                        x--;
                    }
                }
            }

            opcodes.Reverse();
            return OptimizeOpcodes(opcodes);
        }

        private int GetV(Dictionary<int, int> v, int k)
        {
            return v.ContainsKey(k) ? v[k] : -1;
        }

        private List<Opcode> OptimizeOpcodes(List<Opcode> raw)
        {
            if (raw.Count == 0) return raw;

            var result = new List<Opcode>();
            Opcode current = raw[0];

            for (int i = 1; i < raw.Count; i++)
            {
                var next = raw[i];
                if (next.Type == current.Type)
                {
                    // Merge adjacent same ops
                    current.OldEnd = next.OldEnd;
                    current.NewEnd = next.NewEnd;
                }
                else
                {
                    result.Add(current);
                    current = next;
                }
            }
            result.Add(current);
            return result;
        }

        public List<Opcode> GetOpcodes()
        {
            return _opcodes ?? new List<Opcode>();
        }
    }
}