using Microsoft.AspNetCore.Mvc;

namespace HandoraApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    [HttpPost("create-intent/{orderId}")]
    public async Task<IActionResult> CreateIntent(Guid orderId)
    {
        return Ok();
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        return Ok();
    }
}
