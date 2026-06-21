using HandoraApplication.DTOs.AuthDTOs;
using HandoraApplication.DTOs.NotificationsDto;
using HandoraApplication.DTOs.Common;
using HandoraApplication.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HandoraApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly INotificationHubContext _hubContext;

        public NotificationController(INotificationService notificationService, INotificationHubContext hubContext)
        {
            _notificationService = notificationService;
            _hubContext = hubContext;
        }

        [HttpPost("push-realtime")]
        [AllowAnonymous]
        public async Task<IActionResult> PushRealtime([FromBody] PushRealtimeRequest dto, CancellationToken ct)
        {
            var unreadCount = await _notificationService.GetUnreadCountAsync(dto.UserId, ct);
            await _hubContext.SendNotificationAsync(dto.UserId, dto.Notification);
            await _hubContext.SendUnreadCountAsync(dto.UserId, unreadCount);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginationQueryDto query, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _notificationService.GetUserNotificationsAsync(userId, query, ct);
            return Ok(ApiResponse<PagedResultDto<NotificationDto>>.Ok(result));
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var count = await _notificationService.GetUnreadCountAsync(userId, ct);
            return Ok(ApiResponse<int>.Ok(count));
        }

        [HttpPatch("{id:guid}/read")]
        [HttpPut("{id:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
        {
            await _notificationService.MarkAsReadAsync(id, ct);
            return Ok(ApiResponse<object>.Ok(null!, "Marked as read."));
        }

        [HttpPatch("read-all")]
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _notificationService.MarkAllAsReadAsync(userId, ct);
            return Ok(ApiResponse<object>.Ok(null!, "All notifications marked as read."));
        }
    }

    public class PushRealtimeRequest
    {
        public string UserId { get; set; } = string.Empty;
        public HandoraApplication.DTOs.NotificationsDto.NotificationDto Notification { get; set; } = null!;
    }
}
