using HandoraApplication.DTOs.Payments;
using HandoraApplication.IServices;
using HandoraDomain.Models.AppUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HandoraApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PayoutsController(
    IPayoutService payoutService,
    UserManager<User> userManager) : ControllerBase
{
    private readonly IPayoutService _payoutService = payoutService;
    private readonly UserManager<User> _userManager = userManager;
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [Authorize(Roles = AppRoles.Seller)]
    [HttpPost("request")]
    public async Task<IActionResult> RequestWithdrawal([FromBody] CreateWithdrawalDto dto)
    {
        if (dto.Amount <= 0)
            return BadRequest("Amount must be greater than zero");

        var currentUser = await _userManager.FindByIdAsync(CurrentUserId);
        if (currentUser == null)
            return Unauthorized();

        try
        {
            var request = await _payoutService.RequestWithdrawalAsync(currentUser, dto.Amount);
            return CreatedAtAction(nameof(RequestWithdrawal), new { id = request.Id }, request);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("process-pending")]
    public async Task<IActionResult> ProcessPending()
    {
        await _payoutService.ProcessPendingWithdrawalsAsync();
        return Ok("Pending withdrawals processed");
    }
}
