using System;
using System.Collections.Generic;

namespace Linage.Core.Diff
{
    /// <summary>
    /// Minimal diff strategy that produces the smallest possible diff.
    /// Uses a greedy approach to minimize the number of operations.
    /// </summary>
    public class MinimalDiffStrategy : IDiffStrategy
    {
        private List<Opcode> _opcodes;

        public List<Opcode> ComputeDiff(string[] oldLines, string[] newLines)
        {
            _opcodes = new List<Opcode>();
            
            if (oldLines == null) oldLines = new string[0];
            if (newLines == null) newLines = new string[0];

            // Use dynamic programming to find minimal edit distance
            var matrix = BuildEditDistanceMatrix(oldLines, newLines);
            
            // Backtrack to build opcodes
            BacktrackOpcodes(matrix, oldLines, newLines, oldLines.Length, newLines.Length);
            
            // Reverse opcodes (built backwards during backtracking)
            _opcodes.Reverse();
            
            // Merge consecutive operations of the same type
            MergeConsecutiveOps();

            return _opcodes;
        }

        /// <summary>
        /// Build edit distance matrix using dynamic programming.
        /// </summary>
        private int[,] BuildEditDistanceMatrix(string[] oldLines, string[] newLines)
        {
            int m = oldLines.Length;
            int n = newLines.Length;
            var matrix = new int[m + 1, n + 1];

            // Initialize first row and column
            for (int i = 0; i <= m; i++)
                matrix[i, 0] = i;
            for (int j = 0; j <= n; j++)
                matrix[0, j] = j;

            // Fill matrix
            for (int i = 1; i <= m; i++)
            {
                for (int j = 1; j <= n; j++)
                {
                    if (oldLines[i - 1] == newLines[j - 1])
                    {
                        // Lines match - no operation needed
                        matrix[i, j] = matrix[i - 1, j - 1];
                    }
                    else
                    {
                        // Take minimum of insert, delete, or replace
                        int delete = matrix[i - 1, j] + 1;
                        int insert = matrix[i, j - 1] + 1;
                        int replace = matrix[i - 1, j - 1] + 1;
                        
                        matrix[i, j] = Math.Min(Math.Min(delete, insert), replace);
                    }
                }
            }

            return matrix;
        }

        /// <summary>
        /// Backtrack through matrix to build opcodes.
        /// </summary>
        private void BacktrackOpcodes(int[,] matrix, string[] oldLines, string[] newLines, int i, int j)
        {
            if (i == 0 && j == 0)
                return;

            if (i > 0 && j > 0 && oldLines[i - 1] == newLines[j - 1])
            {
                // Lines are equal
                BacktrackOpcodes(matrix, oldLines, newLines, i - 1, j - 1);
                _opcodes.Add(new Opcode
                {
                    Type = OperationType.Equal,
                    OldStart = i - 1,
                    OldEnd = i,
                    NewStart = j - 1,
                    NewEnd = j
                });
            }
            else
            {
                int delete = i > 0 ? matrix[i - 1, j] : int.MaxValue;
                int insert = j > 0 ? matrix[i, j - 1] : int.MaxValue;
                int replace = (i > 0 && j > 0) ? matrix[i - 1, j - 1] : int.MaxValue;

                if (replace <= delete && replace <= insert)
                {
                    // Replace (modify)
                    BacktrackOpcodes(matrix, oldLines, newLines, i - 1, j - 1);
                    _opcodes.Add(new Opcode
                    {
                        Type = OperationType.Modify,
                        OldStart = i - 1,
                        OldEnd = i,
                        NewStart = j - 1,
                        NewEnd = j
                    });
                }
                else if (delete <= insert)
                {
                    // Delete
                    BacktrackOpcodes(matrix, oldLines, newLines, i - 1, j);
                    _opcodes.Add(new Opcode
                    {
                        Type = OperationType.Delete,
                        OldStart = i - 1,
                        OldEnd = i,
                        NewStart = j,
                        NewEnd = j
                    });
                }
                else
                {
                    // Insert
                    BacktrackOpcodes(matrix, oldLines, newLines, i, j - 1);
                    _opcodes.Add(new Opcode
                    {
                        Type = OperationType.Insert,
                        OldStart = i,
                        OldEnd = i,
                        NewStart = j - 1,
                        NewEnd = j
                    });
                }
            }
        }

        /// <summary>
        /// Merge consecutive operations of the same type for cleaner output.
        /// </summary>
        private void MergeConsecutiveOps()
        {
            if (_opcodes.Count <= 1)
                return;

            var merged = new List<Opcode>();
            var current = _opcodes[0];

            for (int i = 1; i < _opcodes.Count; i++)
            {
                var next = _opcodes[i];

                if (current.Type == next.Type && 
                    current.OldEnd == next.OldStart && 
                    current.NewEnd == next.NewStart)
                {
                    // Merge consecutive operations
                    current = new Opcode
                    {
                        Type = current.Type,
                        OldStart = current.OldStart,
                        OldEnd = next.OldEnd,
                        NewStart = current.NewStart,
                        NewEnd = next.NewEnd
                    };
                }
                else
                {
                    merged.Add(current);
                    current = next;
                }
            }

            merged.Add(current);
            _opcodes = merged;
        }

        public List<Opcode> GetOpcodes()
        {
            return _opcodes ?? new List<Opcode>();
        }
    }
}
