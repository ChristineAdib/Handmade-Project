using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.ShopEntities;

namespace HandoraDomain.Models.FollowEntities
{
    public class Follow : BaseEntity<Guid>
    {
        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        public Guid ShopId { get; set; }
        public Shop Shop { get; set; } = null!;

        public DateTime FollowedAt { get; set; } = DateTime.UtcNow;
    }
}