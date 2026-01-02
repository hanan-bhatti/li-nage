using System;
using System.Collections.Generic;

namespace Linage.Core
{
    public class Project
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Description { get; set; }
        public string DefaultBranch { get; set; }
        public string RepositoryPath { get; set; }
        public DateTime CreatedDate { get; set; }
        
        public virtual ICollection<Remote> Remotes { get; set; } = new List<Remote>();

        public Project()
        {
            ProjectId = Guid.NewGuid();
            CreatedDate = DateTime.Now;
        }
    }
}
