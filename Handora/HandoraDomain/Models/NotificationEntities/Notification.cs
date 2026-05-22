using HandoraDomain.Models.AppUser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.NotificationEntities
{
    public class Notification:BaseEntity<Guid>
    {
        // [LOCALIZATION] bilingual title and message
        public string TitleEn { get; set; } = string.Empty;
        public string TitleAr { get; set; } = string.Empty;
        public string MessageEn { get; set; } = string.Empty;
        public string MessageAr { get; set; } = string.Empty;

        public NotificationType Type { get; set; }
        public Guid? ReferenceId { get; set; }          // e.g. OrderId, ReviewId
        public string? ReferenceType { get; set; }      // e.g. "Order", "Review"
        public bool IsRead { get; set; } = false;

        // FK — string because IdentityUser.Id is string
        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;
    }
}
