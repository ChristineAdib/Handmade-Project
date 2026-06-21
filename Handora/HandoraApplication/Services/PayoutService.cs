using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraApplication.DTOs.Payments;
using HandoraDomain.Consts;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.OrderEntity;
using HandoraDomain.Models.PaymentEntities;
using HandoraDomain.Models.ShopEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using HandoraDomain.Models.NotificationEntities;
using HandoraApplication.DTOs.NotificationsDto;

namespace HandoraApplication.Services;

public class PayoutService : IPayoutService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<PayoutService> _logger;
    private readonly INotificationService _notificationService;
    private static readonly HttpClient _httpClient = new();

    public PayoutService(
        IUnitOfWork unitOfWork, 
        IConfiguration configuration,
        UserManager<User> userManager,
        ILogger<PayoutService> logger,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _userManager = userManager;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<WithdrawalRequest> RequestWithdrawalAsync(User seller, decimal amount)
    {
        var shopsRepo = _unitOfWork.Repository<Shop, Guid>();
        var shopsQuery = await shopsRepo.GetAllAsync();
        var shop = await shopsQuery.FirstOrDefaultAsync(s => s.OwnerId == seller.Id);

        if (shop == null)
            throw new InvalidOperationException("Seller does not have a shop");

        // Validate that payout method is configured
        if (string.IsNullOrWhiteSpace(shop.BankName) ||
            string.IsNullOrWhiteSpace(shop.AccountHolderName) ||
            string.IsNullOrWhiteSpace(shop.AccountNumber))
        {
            throw new InvalidOperationException(
                "Please configure your payout method (bank account or visa) before requesting a withdrawal.");
        }

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
            RequestedAt = DateTime.UtcNow,
            Seller = seller,
            Shop = shop
        };

        var requestRepo = _unitOfWork.Repository<WithdrawalRequest, Guid>();
        await requestRepo.AddAsync(request);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Withdrawal request created: {RequestId}, Shop: {ShopId}, Amount: {Amount}",
            request.Id, shop.Id, amount);

        // Execute payout immediately
        try
        {
            var success = await ExecutePayoutAsync(request);
            if (success)
            {
                request.Status = WithdrawalStatus.Paid;
                request.PaidAt = DateTime.UtcNow;

                // Send Notification (Scenario 8)
                try
                {
                    await _notificationService.SendAsync(new SendNotificationDto
                    {
                        UserId = request.SellerId,
                        TitleEn = "Payout Completed",
                        TitleAr = "تم تحويل الأرباح",
                        MessageEn = $"Your withdrawal request of {request.Amount} EGP has been successfully transferred.",
                        MessageAr = $"تم تحويل طلب السحب الخاص بك بقيمة {request.Amount} جنيه بنجاح.",
                        Type = NotificationType.PaymentReceived,
                        ReferenceId = request.Id,
                        ReferenceType = "Payment"
                    });
                }
                catch (System.Exception)
                {
                    // Ignore
                }
            }
            else
            {
                request.Status = WithdrawalStatus.Cancelled;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing payout for request {RequestId}", request.Id);
            request.Status = WithdrawalStatus.Cancelled;
        }

        await requestRepo.UpdateAsync(request);
        await _unitOfWork.SaveChangesAsync();

        return request;
    }

    public async Task ProcessPendingWithdrawalsAsync()
    {
        var requestRepo = _unitOfWork.Repository<WithdrawalRequest, Guid>();
        var query = await requestRepo.GetAllAsync();
        var pendingRequests = await query
            .Include(r => r.Seller)
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

                    // Send Notification (Scenario 8)
                    try
                    {
                        await _notificationService.SendAsync(new SendNotificationDto
                        {
                            UserId = request.SellerId,
                            TitleEn = "Payout Completed",
                            TitleAr = "تم تحويل الأرباح",
                            MessageEn = $"Your withdrawal request of {request.Amount} EGP has been successfully transferred.",
                            MessageAr = $"تم تحويل طلب السحب الخاص بك بقيمة {request.Amount} جنيه بنجاح.",
                            Type = NotificationType.PaymentReceived,
                            ReferenceId = request.Id,
                            ReferenceType = "Payment"
                        });
                    }
                    catch (System.Exception)
                    {
                        // Ignore
                    }
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
            _logger.LogInformation(
                "Executing simulated payout in Test Mode: Request {RequestId}, Amount {Amount}, Seller {SellerId}",
                request.Id, request.Amount, request.SellerId);

            // Simulate small delay for processing
            await Task.Delay(300);

            request.TransferReference = "SIM_PAYOUT_" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            
            _logger.LogInformation(
                "Simulated payout processed successfully for Request {RequestId}. Reference: {Reference}",
                request.Id, request.TransferReference);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payout simulation failed for request {RequestId}", request.Id);
            return false;
        }
    }

    public async Task<Result<SellerWalletDto>> GetSellerWalletAsync(string sellerId)
    {
        try
        {
            var shopsRepo = _unitOfWork.Repository<Shop, Guid>();
            var shopsQuery = await shopsRepo.GetAllAsync();
            var shop = await shopsQuery.FirstOrDefaultAsync(s => s.OwnerId == sellerId);

            if (shop == null)
                return Result<SellerWalletDto>.Failure("Seller does not have a shop");

            var availableBalance = shop.AvailableBalance;

            // Calculate Pending Balance
            // Paid orders that are not yet Delivered, Cancelled or Refunded
            var ordersRepo = _unitOfWork.Repository<Order, Guid>();
            var ordersQuery = await ordersRepo.GetAllAsync();
            var pendingOrders = await ordersQuery
                .Include(o => o.Items)
                .Where(o => o.Items.Any(i => i.ShopId == shop.Id) &&
                            o.PaymentStatus == PaymentStatus.Paid &&
                            o.Status != OrderStatus.Delivered &&
                            o.Status != OrderStatus.Cancelled &&
                            o.Status != OrderStatus.Refunded)
                .ToListAsync();

            decimal pendingBalance = 0;
            foreach (var order in pendingOrders)
            {
                var grossAmount = order.Items.Where(i => i.ShopId == shop.Id).Sum(i => i.Price * i.Quantity);
                var commissionAmount = Math.Round(grossAmount * shop.CommissionRate, 2);
                var netAmount = Math.Round(grossAmount - commissionAmount, 2);
                pendingBalance += netAmount;
            }

            // Calculate Total Earnings (Released + Pending)
            var txRepo = _unitOfWork.Repository<SellerBalanceTransaction, Guid>();
            var txQuery = await txRepo.GetAllAsync();
            var releasedSales = await txQuery
                .Where(t => t.ShopId == shop.Id && t.Type == BalanceTransactionType.Sale && t.IsReleased)
                .SumAsync(t => t.NetAmount);

            var totalEarnings = releasedSales + pendingBalance;

            // Gather transaction history
            var walletTransactions = new List<WalletTransactionDto>();

            // 1. Add all Sales
            var balanceTxList = await txQuery
                .Where(t => t.ShopId == shop.Id)
                .ToListAsync();

            foreach (var tx in balanceTxList)
            {
                walletTransactions.Add(new WalletTransactionDto
                {
                    Id = tx.Id,
                    Type = tx.Type.ToString(),
                    Amount = tx.NetAmount,
                    Date = tx.ReleasedAt ?? tx.CreatedAt,
                    Status = tx.IsReleased ? "Released" : "Pending",
                    Description = tx.Type == BalanceTransactionType.Sale ? $"Sale for Order #{tx.OrderId}" : tx.Type.ToString(),
                    Reference = tx.OrderId.ToString()
                });
            }

            // 2. Add all Withdrawals
            var withdrawalRepo = _unitOfWork.Repository<WithdrawalRequest, Guid>();
            var withdrawalQuery = await withdrawalRepo.GetAllAsync();
            var withdrawals = await withdrawalQuery
                .Where(w => w.ShopId == shop.Id)
                .ToListAsync();

            foreach (var w in withdrawals)
            {
                walletTransactions.Add(new WalletTransactionDto
                {
                    Id = w.Id,
                    Type = "Withdrawal",
                    Amount = -w.Amount,
                    Date = w.RequestedAt,
                    Status = w.Status.ToString(),
                    Description = "Withdrawal Request",
                    Reference = w.TransferReference
                });
            }

            var sortedTransactions = walletTransactions
                .OrderByDescending(t => t.Date)
                .ToList();

            var walletDto = new SellerWalletDto
            {
                AvailableBalance = availableBalance,
                PendingBalance = pendingBalance,
                TotalEarnings = totalEarnings,
                Transactions = sortedTransactions
            };

            return Result<SellerWalletDto>.Success(walletDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seller wallet for seller {SellerId}", sellerId);
            return Result<SellerWalletDto>.Failure($"Failed to retrieve wallet details: {ex.Message}");
        }
    }

    public async Task<Result<BankAccountDto>> GetBankAccountAsync(string sellerId)
    {
        try
        {
            var shopsRepo = _unitOfWork.Repository<Shop, Guid>();
            var shopsQuery = await shopsRepo.GetAllAsync();
            var shop = await shopsQuery.FirstOrDefaultAsync(s => s.OwnerId == sellerId);

            if (shop == null)
                return Result<BankAccountDto>.Failure("Seller does not have a shop");

            var dto = new BankAccountDto
            {
                BankName = shop.BankName ?? string.Empty,
                AccountHolderName = shop.AccountHolderName ?? string.Empty,
                AccountNumber = shop.AccountNumber ?? string.Empty
            };

            return Result<BankAccountDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bank account for seller {SellerId}", sellerId);
            return Result<BankAccountDto>.Failure($"Failed to retrieve bank account: {ex.Message}");
        }
    }

    public async Task<Result<bool>> UpdateBankAccountAsync(string sellerId, BankAccountDto dto)
    {
        try
        {
            var shopsRepo = _unitOfWork.Repository<Shop, Guid>();
            var shopsQuery = await shopsRepo.GetAllAsync();
            var shop = await shopsQuery.FirstOrDefaultAsync(s => s.OwnerId == sellerId);

            if (shop == null)
                return Result<bool>.Failure("Seller does not have a shop");

            shop.BankName = dto.BankName;
            shop.AccountHolderName = dto.AccountHolderName;
            shop.AccountNumber = dto.AccountNumber;

            await shopsRepo.UpdateAsync(shop);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Bank account updated for shop {ShopId}", shop.Id);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bank account for seller {SellerId}", sellerId);
            return Result<bool>.Failure($"Failed to update bank account: {ex.Message}");
        }
    }
}
