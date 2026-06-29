using System;

namespace HandoraDomain.Models.CustomStudioEntities
{
    public class WorkspaceTimelineEntry : BaseEntity<Guid>
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsCompleted { get; set; }

        // Relationship
        public Guid ProjectWorkspaceId { get; set; }
        public ProjectWorkspace ProjectWorkspace { get; set; } = null!;
    }
}
