using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.PaymentEntities;
using HandoraDomain.Models.ShopEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HandoraApplication.Services;

public class PayoutService : IPayoutService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PayoutService> _logger;

    public PayoutService(IUnitOfWork unitOfWork, ILogger<PayoutService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<WithdrawalRequest> RequestWithdrawalAsync(User seller, decimal amount)
    {
        var shopsRepo = _unitOfWork.Repository<Shop, Guid>();
        var shopsQuery = await shopsRepo.GetAllAsync();
        var shop = await shopsQuery.FirstOrDefaultAsync(s => s.OwnerId == seller.Id);

        if (shop == null)
            throw new InvalidOperationException("Seller does not have a shop");

        if (shop.AvailableBalance < amount)
            throw new InvalidOperationException(
                $"Insufficient balance. Available: {shop.AvailableBalance}, Requested: {amount}");

        // Deduct from balance immediately to prevent double-withdrawal
        shop.AvailableBalance -= amount;
        await shopsRepo.UpdateAsync(shop);

        var request = new WithdrawalRequest
        {
            ShopId = shop.Id,
            SellerId = seller.Id,
            Amount = amount,
            Status = WithdrawalStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        var requestRepo = _unitOfWork.Repository<WithdrawalRequest, Guid>();
        await requestRepo.AddAsync(request);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Withdrawal request created: {RequestId}, Shop: {ShopId}, Amount: {Amount}",
            request.Id, shop.Id, amount);

        return request;
    }

    public async Task ProcessPendingWithdrawalsAsync()
    {
        var requestRepo = _unitOfWork.Repository<WithdrawalRequest, Guid>();
        var query = await requestRepo.GetAllAsync();
        var pendingRequests = await query
            .Where(r => r.Status == WithdrawalStatus.Pending)
            .ToListAsync();

        _logger.LogInformation("Processing {Count} pending withdrawal requests", pendingRequests.Count);

        foreach (var request in pendingRequests)
        {
            try
            {
                var success = await ExecutePayoutAsync(request);
                if (success)
                {
                    request.Status = WithdrawalStatus.Paid;
                    request.PaidAt = DateTime.UtcNow;
                    await requestRepo.UpdateAsync(request);
                    _logger.LogInformation("Withdrawal {RequestId} processed successfully", request.Id);
                }
                else
                {
                    request.Status = WithdrawalStatus.Cancelled;
                    await requestRepo.UpdateAsync(request);
                    _logger.LogWarning("Withdrawal {RequestId} failed, marked as cancelled", request.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing withdrawal {RequestId}", request.Id);
                request.Status = WithdrawalStatus.Cancelled;
                await requestRepo.UpdateAsync(request);
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<bool> ExecutePayoutAsync(WithdrawalRequest request)
    {
        try
        {
            // In production: call external payment API (e.g., bank transfer, Paymob payout)
            // For now: record the payout as successful

            _logger.LogInformation(
                "Executing payout: Request {RequestId}, Amount {Amount}, Seller {SellerId}",
                request.Id, request.Amount, request.SellerId);

            // Simulate payout processing
            await Task.Delay(100);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payout execution failed for request {RequestId}", request.Id);

            // Revert balance deduction
            var shopsRepo = _unitOfWork.Repository<Shop, Guid>();
            var shopsQuery = await shopsRepo.GetAllAsync();
            var shop = await shopsQuery.FirstOrDefaultAsync(s => s.Id == request.ShopId);
            if (shop != null)
            {
                shop.AvailableBalance += request.Amount;
                await shopsRepo.UpdateAsync(shop);
                await _unitOfWork.SaveChangesAsync();
            }

            return false;
        }
    }
}
