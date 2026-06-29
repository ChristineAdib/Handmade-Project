using System;
using HandoraDomain.Models.ShopEntities;

namespace HandoraDomain.Models.CustomStudioEntities
{
    public class SellerRecommendation : BaseEntity<Guid>
    {
        public double MatchingScore { get; set; }
        public string Reason { get; set; } = string.Empty;
        public decimal EstimatedPrice { get; set; }
        public int EstimatedDeliveryDays { get; set; }

        // Relationships
        public Guid ShopId { get; set; }
        public Shop Shop { get; set; } = null!;

        public Guid CustomRequestId { get; set; }
        public CustomRequest CustomRequest { get; set; } = null!;
    }
}
