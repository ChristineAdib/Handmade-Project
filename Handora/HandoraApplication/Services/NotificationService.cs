using HandoraApplication.DTOs.NotificationsDto;
using HandoraApplication.DTOs.Common;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.NotificationEntities;
using Microsoft.Extensions.Logging;
namespace HandoraApplication.Services
{
    public sealed class NotificationService(
        INotificationRepository repo,
        INotificationHubContext hubContext,
        ILogger<NotificationService> logger) : INotificationService
    {
        private readonly INotificationRepository _repo = repo;
        private readonly INotificationHubContext _hubContext = hubContext; 
        private readonly ILogger<NotificationService> _logger = logger;

        public async Task SendAsync(SendNotificationDto dto, CancellationToken ct = default)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                TitleEn = dto.TitleEn,
                TitleAr = dto.TitleAr,
                MessageEn = dto.MessageEn,
                MessageAr = dto.MessageAr,
                Type = dto.Type,
                ReferenceId = dto.ReferenceId,
                ReferenceType = dto.ReferenceType,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(notification, ct);
            await _repo.SaveChangesAsync(ct);

            var notificationDto = MapToDto(notification);
            var unreadCount = await _repo.GetUnreadCountAsync(dto.UserId, ct);

            await _hubContext.SendNotificationAsync(dto.UserId, notificationDto);
            await _hubContext.SendUnreadCountAsync(dto.UserId, unreadCount);

            _logger.LogInformation(
                "Notification sent to user {UserId} — Type: {Type}", dto.UserId, dto.Type);
        }

        public async Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(
            string userId, CancellationToken ct = default)
        {
            var notifications = await _repo.GetUserNotificationsAsync(userId, ct);
            return notifications.Select(MapToDto).ToList();
        }

        public async Task<PagedResultDto<NotificationDto>> GetUserNotificationsAsync(
            string userId, PaginationQueryDto query, CancellationToken ct = default)
        {
            var (items, totalCount) = await _repo.GetUserNotificationsPagedAsync(
                userId, query.PageNumber, query.PageSize, ct);

            return new PagedResultDto<NotificationDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }

        public Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default)
            => _repo.GetUnreadCountAsync(userId, ct);

        public async Task MarkAsReadAsync(Guid notificationId, CancellationToken ct = default)
            => await _repo.MarkAsReadAsync(notificationId, ct);

        public async Task MarkAllAsReadAsync(string userId, CancellationToken ct = default)
            => await _repo.MarkAllAsReadAsync(userId, ct);

        private static NotificationDto MapToDto(Notification n) => new()
        {
            Id = n.Id,
            TitleEn = n.TitleEn,
            TitleAr = n.TitleAr,
            MessageEn = n.MessageEn,
            MessageAr = n.MessageAr,
            Type = n.Type,
            ReferenceId = n.ReferenceId,
            ReferenceType = n.ReferenceType,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        };
    }
}
