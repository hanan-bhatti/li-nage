using System;
using System.Linq;
using Linage.Infrastructure;

namespace Linage.Core
{
    public class AIActivityService
    {
        private readonly LiNageDbContext _context;

        public AIActivityService(LiNageDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void LogActivity(AIActivity activity)
        {
            if (activity == null) throw new ArgumentNullException(nameof(activity));

            _context.AIActivities.Add(activity);
            _context.SaveChanges();
        }

        public void LogActivity(string toolName, AssistanceLevel level, string description, int linesAffected, Guid? commitId = null, float confidence = 1.0f)
        {
            var activity = new AIActivity
            {
                AITool = toolName,
                AssistanceLevel = level,
                Description = description,
                LinesAffected = linesAffected,
                Confidence = confidence,
                CommitId = commitId ?? Guid.Empty // Associate with commit if available
            };

            LogActivity(activity);
        }

        public System.Collections.Generic.List<AIActivity> GetRecentActivities(int count = 10)
        {
            return _context.AIActivities
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToList();
        }
    }
}
