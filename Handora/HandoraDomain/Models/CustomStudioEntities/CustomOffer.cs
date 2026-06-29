using System;
using HandoraDomain.Consts;
using HandoraDomain.Models.ShopEntities;
using HandoraDomain.Models.ChatEntities;
using HandoraDomain.Models.OrderEntity;

namespace HandoraDomain.Models.CustomStudioEntities
{
    public class CustomOffer : BaseEntity<Guid>
    {
        public decimal Price { get; set; }
        public int DeliveryTimeDays { get; set; }
        public int RevisionsAllowed { get; set; }
        public string AttachmentsJson { get; set; } = string.Empty; // JSON list of file URLs
        public string Notes { get; set; } = string.Empty;
        public OfferStatus Status { get; set; } = OfferStatus.Pending;

        // Relationships
        public Guid ShopId { get; set; }
        public Shop Shop { get; set; } = null!;

        public Guid CustomRequestId { get; set; }
        public CustomRequest CustomRequest { get; set; } = null!;

        public Guid? ConversationId { get; set; }
        public Conversation? Conversation { get; set; }

        public string BuyerId { get; set; } = string.Empty;
        public string SellerId { get; set; } = string.Empty;

        public Guid? DesignId { get; set; }
        public GeneratedDesign? Design { get; set; }

        public DateTime? AcceptedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public DateTime? PaidAt { get; set; }

        public Guid? OrderId { get; set; }
        public Order? Order { get; set; }

        public Guid? WorkspaceId { get; set; }
        public ProjectWorkspace? Workspace { get; set; }
    }
}
