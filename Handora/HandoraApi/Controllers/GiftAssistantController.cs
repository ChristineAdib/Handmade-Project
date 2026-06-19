using HandoraApplication.AI.DTOs;
using HandoraApplication.AI.Interfaces;
using HandoraApplication.DTOs.AuthDTOs;
using Microsoft.AspNetCore.Mvc;

namespace HandoraApi.Controllers;

[ApiController]
[Route("api/gift-assistant")]
public class GiftAssistantController(IGiftAssistantService giftAssistantService) : ControllerBase
{
    private readonly IGiftAssistantService _giftAssistantService = giftAssistantService;

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] GiftChatRequestDto request)
    {
        var response = await _giftAssistantService.ProcessChatAsync(request);
        return Ok(ApiResponse<GiftChatResponseDto>.Ok(response));
    }

    [HttpPost("reset")]
    public async Task<IActionResult> ResetSession([FromBody] ResetSessionDto request)
    {
        await _giftAssistantService.ResetSessionAsync(request.SessionId);
        return Ok(ApiResponse<object>.Ok(null!, "Session reset successfully."));
    }
}

public class ResetSessionDto
{
    public string SessionId { get; set; } = string.Empty;
}
