using HandoraApplication.DTOs.AuthDTOs;
using HandoraApplication.DTOs.ChatDTOs;
using HandoraApplication.IServices;
using HandoraDomain.Models.AppUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HandoraApi.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
            => _chatService = chatService;

        [HttpPost("start")]
        [Authorize(Roles = AppRoles.Buyer)] 
        public async Task<IActionResult> StartConversation(
            [FromBody] StartConversationDto dto, CancellationToken ct)
        {
            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _chatService.StartConversationAsync(buyerId, dto.SellerId, ct);
            return Ok(ApiResponse<ConversationDto>.Ok(result));
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _chatService.GetUserConversationsAsync(userId, ct);
            return Ok(ApiResponse<IReadOnlyList<ConversationDto>>.Ok(result));
        }

        [HttpGet("{conversationId:guid}/messages")]
        public async Task<IActionResult> GetMessages(Guid conversationId, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _chatService.GetMessagesAsync(conversationId, userId, ct);
            return Ok(ApiResponse<IReadOnlyList<MessageDto>>.Ok(result));
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage(
            [FromBody] SendMessageDto dto, CancellationToken ct)
        {
            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _chatService.SendMessageAsync(senderId, dto, ct);
            return Ok(ApiResponse<MessageDto>.Ok(result, "Message sent."));
        }

        [HttpPatch("{conversationId:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid conversationId, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _chatService.MarkAsReadAsync(conversationId, userId, ct);
            return Ok(ApiResponse<object>.Ok(null!, "Messages marked as read."));
        }
    }
}
