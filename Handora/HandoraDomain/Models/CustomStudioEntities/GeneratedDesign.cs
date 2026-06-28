using System;

namespace HandoraDomain.Models.CustomStudioEntities
{
    public class GeneratedDesign : BaseEntity<Guid>
    {
        public string ImageUrl { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty; // e.g. "Google AI Studio"
        public long GenerationTimeMs { get; set; }
        public double MatchingScore { get; set; }
        
        public bool IsSelected { get; set; }
        public bool IsSaved { get; set; }
        public bool IsDownloaded { get; set; }
        public bool IsLocked { get; set; }

        public string PatternStepsMarkdown { get; set; } = string.Empty;
        public string DesignSummaryJson { get; set; } = string.Empty;

        // Relationship
        public Guid CustomRequestId { get; set; }
        public CustomRequest CustomRequest { get; set; } = null!;
    }
}
