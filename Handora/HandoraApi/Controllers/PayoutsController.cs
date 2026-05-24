using HandoraApplication.DTOs.Payments;
using Microsoft.AspNetCore.Mvc;

namespace HandoraApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PayoutsController : ControllerBase
{
    [HttpPost("request")]
    public async Task<IActionResult> RequestWithdrawal([FromBody] CreateWithdrawalDto dto)
    {
        return Ok();
    }

    [HttpPost("process-pending")]
    public async Task<IActionResult> ProcessPending()
    {
        return Ok();
    }
}
