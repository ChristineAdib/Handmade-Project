using System;
using HandoraDomain.Consts;
using HandoraDomain.Models.ShopEntities;

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
    }
}
