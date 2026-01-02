using System;
using System.Collections.Generic;
using Linage.Core;
using Linage.Infrastructure;

namespace Linage.Controllers
{
    public class AIActivityController
    {
        public string Status { get; set; } = "Idle";
        public DateTime LastRunTime { get; set; }

        public AIAccessLog LogService { get; set; }
        private readonly AIActivityService _aiActivityService;
        
        public AIActivityController()
        {
            LogService = new AIAccessLog();
            _aiActivityService = new AIActivityService(new LiNageDbContext());
        }
        
        /// <summary>
        /// Log AI-assisted code activity
        /// </summary>
        public void LogAIActivity(string toolName, AssistanceLevel level, string description, int linesAffected, Guid? commitId = null, float confidence = 1.0f)
        {
            if (string.IsNullOrEmpty(toolName))
                throw new ArgumentNullException(nameof(toolName));
                
            _aiActivityService.LogActivity(toolName, level, description, linesAffected, commitId, confidence);
            Status = $"Logged: {toolName}";
            LastRunTime = DateTime.Now;
        }
        
        /// <summary>
        /// Get recent AI activities
        /// </summary>
        public List<AIActivity> GetRecentActivities(int count = 10)
        {
            return _aiActivityService.GetRecentActivities(count);
        }
        
        /// <summary>
        /// Track when AI assistance is used in the editor
        /// </summary>
        public void TrackEditorAssistance(string toolName, string codeSnippet, int lineCount)
        {
            LogAIActivity(
                toolName: toolName,
                level: AssistanceLevel.COMPLETION,
                description: $"Code completion: {codeSnippet.Substring(0, Math.Min(50, codeSnippet.Length))}...",
                linesAffected: lineCount,
                confidence: 0.85f
            );
        }
    }
}
