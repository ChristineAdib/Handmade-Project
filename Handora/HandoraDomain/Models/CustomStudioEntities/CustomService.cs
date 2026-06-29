using System;
using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.ShopEntities;
using HandoraDomain.Models.ChatEntities;

namespace HandoraDomain.Models.CustomStudioEntities
{
    public class CustomService : BaseEntity<Guid>
    {
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int EstimatedDeliveryDays { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending Buyer Approval"; // Pending Buyer Approval, Approved, Rejected

        // Relationships
        public string BuyerId { get; set; } = string.Empty;
        public User Buyer { get; set; } = null!;

        public string SellerId { get; set; } = string.Empty;
        public User Seller { get; set; } = null!;

        public Guid ShopId { get; set; }
        public Shop Shop { get; set; } = null!;

        public Guid ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;

        public Guid CustomRequestId { get; set; }
        public CustomRequest CustomRequest { get; set; } = null!;

        public Guid? GeneratedDesignId { get; set; }
        public GeneratedDesign? GeneratedDesign { get; set; }

        public Guid? OrderId { get; set; }
    }
}
