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

    // [Authorize(Roles = AppRoles.Seller)]
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
            return CreatedAtAction(nameof(RequestWithdrawal), new { id = request.Id }, new {
                request.Id,
                request.ShopId,
                request.SellerId,
                request.Amount,
                Status = request.Status.ToString(),
                request.RequestedAt,
                request.PaidAt,
                request.TransferReference
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // [Authorize(Roles = AppRoles.Seller)]
    [HttpGet("wallet")]
    public async Task<IActionResult> GetSellerWallet()
    {
        var currentUser = await _userManager.FindByIdAsync(CurrentUserId);
        if (currentUser == null)
            return Unauthorized();

        var result = await _payoutService.GetSellerWalletAsync(currentUser.Id);
        if (!result.IsSuccess)
            return BadRequest(result.Errors);

        return Ok(result.Data);
    }

    // [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("process-pending")]
    public async Task<IActionResult> ProcessPending()
    {
        await _payoutService.ProcessPendingWithdrawalsAsync();
        return Ok("Pending withdrawals processed");
    }

    // [Authorize(Roles = AppRoles.Seller)]
    [HttpGet("bank-account")]
    public async Task<IActionResult> GetBankAccount()
    {
        var currentUser = await _userManager.FindByIdAsync(CurrentUserId);
        if (currentUser == null)
            return Unauthorized();

        var result = await _payoutService.GetBankAccountAsync(currentUser.Id);
        if (!result.IsSuccess)
            return BadRequest(result.Errors);

        return Ok(result.Data);
    }

    // [Authorize(Roles = AppRoles.Seller)]
    [HttpPost("bank-account")]
    public async Task<IActionResult> UpdateBankAccount([FromBody] BankAccountDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.BankName) ||
            string.IsNullOrWhiteSpace(dto.AccountHolderName) ||
            string.IsNullOrWhiteSpace(dto.AccountNumber))
        {
            return BadRequest("All bank account fields are required.");
        }

        var currentUser = await _userManager.FindByIdAsync(CurrentUserId);
        if (currentUser == null)
            return Unauthorized();

        var result = await _payoutService.UpdateBankAccountAsync(currentUser.Id, dto);
        if (!result.IsSuccess)
            return BadRequest(result.Errors);

        return Ok(new { message = "Bank account updated successfully" });
    }
}
