using HandoraDomain.Models.AppUser;
using System;

namespace HandoraDomain.Models.ShopEntities
{
    public class ShopReview : BaseEntity<Guid>
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsApproved { get; set; } = true;

        // FKs
        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        public Guid ShopId { get; set; }
        public Shop Shop { get; set; } = null!;
    }
}
