using HandoraDomain.Models.NotificationEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.NotificationsDto
{
    public sealed record NotificationDto
    {
        public Guid Id { get; init; }
        public string TitleEn { get; init; } = string.Empty;
        public string TitleAr { get; init; } = string.Empty;
        public string MessageEn { get; init; } = string.Empty;
        public string MessageAr { get; init; } = string.Empty;
        public NotificationType Type { get; init; }
        public Guid? ReferenceId { get; init; }
        public string? ReferenceType { get; init; }
        public bool IsRead { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
