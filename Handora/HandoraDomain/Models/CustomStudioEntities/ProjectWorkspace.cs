using System;
using HandoraDomain.Consts;
using HandoraDomain.Models.ChatEntities;
using HandoraDomain.Models.PaymentEntities;

namespace HandoraDomain.Models.CustomStudioEntities
{
    public class ProjectWorkspace : BaseEntity<Guid>
    {
        public ProjectWorkspaceStatus Status { get; set; } = ProjectWorkspaceStatus.Initiated;
        public int MilestoneStep { get; set; } = 1;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        public string? FinalPhotoUrl { get; set; }
        public string? TrackingNumber { get; set; }
        public bool IsLocked { get; set; }

        public Guid? CustomServiceId { get; set; }
        public CustomService? CustomService { get; set; }

        public Guid? OrderId { get; set; }
        public OrderEntity.Order? Order { get; set; }

        public System.Collections.Generic.ICollection<WorkspaceTimelineEntry> TimelineEntries { get; set; } = new System.Collections.Generic.List<WorkspaceTimelineEntry>();

        // Relationships
        public Guid? SelectedOfferId { get; set; }
        public CustomOffer? SelectedOffer { get; set; }

        public Guid CustomRequestId { get; set; }
        public CustomRequest CustomRequest { get; set; } = null!;

        public Guid? ChatConversationId { get; set; }
        public Conversation? ChatConversation { get; set; }
    }
}
