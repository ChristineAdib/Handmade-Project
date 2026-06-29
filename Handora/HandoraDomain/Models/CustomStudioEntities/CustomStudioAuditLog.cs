using System;

namespace HandoraDomain.Models.CustomStudioEntities
{
    public class CustomStudioAuditLog : BaseEntity<Guid>
    {
        public Guid? RequestId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? BuyerId { get; set; }
        public string? SellerId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
