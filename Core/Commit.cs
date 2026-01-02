using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Linage.Core
{
    /// <summary>
    /// Represents an immutable version snapshot.
    /// Spec: 4.2.1
    /// </summary>
    public class Commit
    {
        public Guid CommitId { get; set; } = Guid.NewGuid();
        public string AuthorName { get; set; }
        public string AuthorEmail { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public List<Commit> Parents { get; set; } = new List<Commit>();
        public Snapshot Snapshot { get; set; }
        public string CommitHash { get; set; }
        public bool AiAssisted { get; set; }

        public string GetAuthorSignature()
        {
            return $"{AuthorName} <{AuthorEmail}>";
        }

        public bool IsMultiParent()
        {
            return Parents != null && Parents.Count > 1;
        }

        public bool IsMergeCommit()
        {
            return IsMultiParent();
        }

        public string CalculateHash()
        {
            // Simple implementation: Hash of (Author, Message, Timestamp, SnapshotHash, ParentHashes)
            using (var sha256 = SHA256.Create())
            {
                var sb = new StringBuilder();
                sb.Append(GetAuthorSignature());
                sb.Append(Message);
                sb.Append(Timestamp.ToString("O"));
                if (Snapshot != null) sb.Append(Snapshot.Hash);
                
                if (Parents != null)
                {
                    foreach (var p in Parents.OrderBy(x => x.CommitId))
                    {
                        sb.Append(p.CommitHash ?? p.CommitId.ToString());
                    }
                }

                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }

        public List<Commit> GetAllParents()
        {
            var allParents = new HashSet<Commit>();
            var stack = new Stack<Commit>();
            
            if (Parents != null)
                foreach (var p in Parents) stack.Push(p);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (allParents.Add(current))
                {
                    if (current.Parents != null)
                        foreach (var p in current.Parents) stack.Push(p);
                }
            }
            
            return allParents.ToList();
        }
    }
}