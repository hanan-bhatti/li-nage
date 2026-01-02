using System;
using System.Collections.Generic;

namespace Linage.Core.Diff
{
    public class PatientDiffStrategy : IDiffStrategy
    {
        private List<Opcode> _opcodes;

        public List<Opcode> ComputeDiff(string[] oldLines, string[] newLines)
        {
            _opcodes = new List<Opcode>();
            ComputePatienceDiff(oldLines, newLines, 0, 0, oldLines.Length, newLines.Length);
            return _opcodes;
        }

        private void ComputePatienceDiff(string[] oldLines, string[] newLines, int oldStart, int newStart, int oldLen, int newLen)
        {
            // 1. Find unique lines in both ranges
            var uniqueInOld = FindUniqueLines(oldLines, oldStart, oldLen);
            var uniqueInNew = FindUniqueLines(newLines, newStart, newLen);

            // 2. Find matches between unique lines
            var matches = new List<Match>();
            foreach (var kvp in uniqueInOld)
            {
                if (uniqueInNew.TryGetValue(kvp.Key, out int newIndex))
                {
                    matches.Add(new Match { OldIndex = kvp.Value, NewIndex = newIndex });
                }
            }

            // 3. Find Longest Increasing Subsequence of matches
            var lis = FindLIS(matches);

            if (lis.Count == 0)
            {
                // Fallback to Myers if no unique anchors found
                ProcessFallback(oldLines, newLines, oldStart, newStart, oldLen, newLen);
                return;
            }

            // 4. Recurse between anchors
            int currentOld = oldStart;
            int currentNew = newStart;

            foreach (var match in lis)
            {
                // Diff before anchor
                int subOldLen = match.OldIndex - currentOld;
                int subNewLen = match.NewIndex - currentNew;
                
                if (subOldLen > 0 || subNewLen > 0)
                {
                    ComputePatienceDiff(oldLines, newLines, currentOld, currentNew, subOldLen, subNewLen);
                }

                // Add anchor as Equal
                _opcodes.Add(new Opcode
                {
                    Type = OperationType.Equal,
                    OldStart = match.OldIndex,
                    OldEnd = match.OldIndex + 1,
                    NewStart = match.NewIndex,
                    NewEnd = match.NewIndex + 1
                });

                currentOld = match.OldIndex + 1;
                currentNew = match.NewIndex + 1;
            }

            // Diff after last anchor
            int tailOldLen = (oldStart + oldLen) - currentOld;
            int tailNewLen = (newStart + newLen) - currentNew;

            if (tailOldLen > 0 || tailNewLen > 0)
            {
                ComputePatienceDiff(oldLines, newLines, currentOld, currentNew, tailOldLen, tailNewLen);
            }
        }

        private void ProcessFallback(string[] oldLines, string[] newLines, int oldStart, int newStart, int oldLen, int newLen)
        {
            // Extract subarrays
            var subOld = new string[oldLen];
            Array.Copy(oldLines, oldStart, subOld, 0, oldLen);
            var subNew = new string[newLen];
            Array.Copy(newLines, newStart, subNew, 0, newLen);

            // Use Myers for the block
            var myers = new MyersDiffStrategy();
            var subOpcodes = myers.ComputeDiff(subOld, subNew);

            // Adjust indices and add
            foreach (var opItem in subOpcodes)
            {
                var op = opItem; // Create mutable copy
                op.OldStart += oldStart;
                op.OldEnd += oldStart;
                op.NewStart += newStart;
                op.NewEnd += newStart;
                _opcodes.Add(op);
            }
        }

        private Dictionary<string, int> FindUniqueLines(string[] lines, int start, int len)
        {
            var counts = new Dictionary<string, int>();
            var indices = new Dictionary<string, int>();

            for (int i = start; i < start + len; i++)
            {
                var line = lines[i];
                if (!counts.ContainsKey(line))
                {
                    counts[line] = 0;
                    indices[line] = i;
                }
                counts[line]++;
            }

            var result = new Dictionary<string, int>();
            foreach (var kvp in counts)
            {
                if (kvp.Value == 1)
                {
                    result[kvp.Key] = indices[kvp.Key];
                }
            }
            return result;
        }

        private List<Match> FindLIS(List<Match> matches)
        {
            // Sort by OldIndex to ensure we process in order
            matches.Sort((a, b) => a.OldIndex.CompareTo(b.OldIndex));

            // Standard O(N log N) LIS on NewIndex
            if (matches.Count == 0) return new List<Match>();

            var tails = new int[matches.Count];
            var parent = new int[matches.Count]; // To reconstruct
            var indexInTails = new int[matches.Count]; // Index in matches for the tail
            int length = 0;

            for (int i = 0; i < matches.Count; i++)
            {
                // Binary search for matches[i].NewIndex in tails
                int low = 0, high = length;
                while (low < high)
                {
                    int mid = low + (high - low) / 2;
                    if (matches[indexInTails[mid]].NewIndex < matches[i].NewIndex)
                        low = mid + 1;
                    else
                        high = mid;
                }

                // Append or replace
                if (low == length) length++;
                
                tails[low] = matches[i].NewIndex; // value not strictly needed if we track index
                indexInTails[low] = i;
                parent[i] = (low > 0) ? indexInTails[low - 1] : -1;
            }

            // Reconstruct path
            var result = new List<Match>();
            int curr = indexInTails[length - 1];
            while (curr != -1)
            {
                result.Add(matches[curr]);
                curr = parent[curr];
            }
            result.Reverse();
            return result;
        }

        private class Match
        {
            public int OldIndex;
            public int NewIndex;
        }

        public List<Opcode> GetOpcodes()
        {
            return _opcodes ?? new List<Opcode>();
        }
    }
}
