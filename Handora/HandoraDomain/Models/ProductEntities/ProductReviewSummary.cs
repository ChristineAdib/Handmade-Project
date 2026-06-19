using System;

namespace HandoraDomain.Models.ProductEntities
{
    public class ProductReviewSummary : BaseEntity<Guid>
    {
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public string OverallSummary { get; set; } = string.Empty;
        
        // Stored as serialized JSON array
        public string Pros { get; set; } = "[]";
        
        // Stored as serialized JSON array
        public string Cons { get; set; } = "[]";

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
