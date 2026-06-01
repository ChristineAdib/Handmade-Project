using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Consts;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.OrderEntity;
using HandoraDomain.Models.PaymentEntities;
using HandoraDomain.Models.ShopEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HandoraApplication.Services;

public class EscrowService : IEscrowService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommissionService _commissionService;
    private readonly ILogger<EscrowService> _logger;

    public EscrowService(
        IUnitOfWork unitOfWork,
        ICommissionService commissionService,
        ILogger<EscrowService> logger)
    {
        _unitOfWork = unitOfWork;
        _commissionService = commissionService;
        _logger = logger;
    }

    public async Task<Result> RecordDeliveryAsync(Guid orderId)
    {
        try
        {
            var ordersRepo = _unitOfWork.Repository<Order, Guid>();
            var query = await ordersRepo.GetAllAsync();
            var order = await query
                .Include(o => o.User)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return Result.Failure("Order not found");

            if (order.PaymentStatus != PaymentStatus.Paid)
                return Result.Failure("Order has not been paid yet");

            if (order.Status == OrderStatus.Delivered)
                return Result.Failure("Order is already delivered");

            if (order.Status is OrderStatus.Cancelled or OrderStatus.Refunded)
                return Result.Failure("Cannot deliver a cancelled or refunded order");

            // Find the shop from the first order item
            var firstItem = order.Items.FirstOrDefault();
            if (firstItem == null)
                return Result.Failure("Order has no items");

            var shopsRepo = _unitOfWork.Repository<Shop, Guid>();
            var shopsQuery = await shopsRepo.GetAllAsync();
            var shop = await shopsQuery.FirstOrDefaultAsync(s => s.Id == firstItem.ShopId);

            if (shop == null)
                return Result.Failure("Could not determine shop for this order");

            // Calculate amounts
            var grossAmount = order.SubTotal;
            var commissionAmount = _commissionService.CalculateCommission(grossAmount, shop.CommissionRate);
            var netAmount = _commissionService.CalculateSellerNet(grossAmount, commissionAmount);

            // Create balance transaction (held in escrow)
            var holdPeriodDays = 14;
            var transaction = new SellerBalanceTransaction
            {
                SellerId = shop.OwnerId,
                ShopId = shop.Id,
                OrderId = order.Id,
                GrossAmount = grossAmount,
                CommissionAmount = commissionAmount,
                NetAmount = netAmount,
                Type = BalanceTransactionType.Sale,
                IsReleased = false,
                HoldUntil = DateTime.UtcNow.AddDays(holdPeriodDays)
            };

            var transactionRepo = _unitOfWork.Repository<SellerBalanceTransaction, Guid>();
            await transactionRepo.AddAsync(transaction);

            // Update order
            order.Status = OrderStatus.Delivered;
            order.DeliveredAt = DateTime.UtcNow;
            order.SellerAmount = netAmount;
            order.PlatformCommission = commissionAmount;
            order.TotalAmount = grossAmount;

            await ordersRepo.UpdateAsync(order);

            // Update shop pending balance
            shop.PendingBalance += netAmount;
            await shopsRepo.UpdateAsync(shop);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Delivery recorded for order {OrderId}. Net amount {NetAmount} held until {HoldUntil}",
                order.Id, netAmount, transaction.HoldUntil);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording delivery for order {OrderId}", orderId);
            return Result.Failure($"Failed to record delivery: {ex.Message}");
        }
    }

    public async Task<Result> CheckAndReleaseFundsAsync()
    {
        try
        {
            var transactionRepo = _unitOfWork.Repository<SellerBalanceTransaction, Guid>();
            var shopsRepo = _unitOfWork.Repository<Shop, Guid>();
            var ordersRepo = _unitOfWork.Repository<Order, Guid>();

            var transactionsQuery = await transactionRepo.GetAllAsync();
            var ordersQuery = await ordersRepo.GetAllAsync();
            var shopsQuery = await shopsRepo.GetAllAsync();

            var releasedTransactions = await transactionsQuery
                .Where(t => !t.IsReleased && t.HoldUntil <= DateTime.UtcNow)
                .ToListAsync();

            if (releasedTransactions.Count == 0)
            {
                _logger.LogInformation("No funds to release");
                return Result.Success();
            }

            foreach (var transaction in releasedTransactions)
            {
                var shop = await shopsQuery.FirstOrDefaultAsync(s => s.Id == transaction.ShopId);
                var order = await ordersQuery.FirstOrDefaultAsync(o => o.Id == transaction.OrderId);

                if (shop == null || order == null)
                {
                    _logger.LogWarning("Shop or order not found for transaction {TransactionId}", transaction.Id);
                    continue;
                }

                shop.PendingBalance -= transaction.NetAmount;
                shop.AvailableBalance += transaction.NetAmount;

                transaction.IsReleased = true;
                transaction.ReleasedAt = DateTime.UtcNow;

                order.IsFundsReleased = true;

                await shopsRepo.UpdateAsync(shop);
                await ordersRepo.UpdateAsync(order);
                await transactionRepo.UpdateAsync(transaction);

                _logger.LogInformation(
                    "Funds released for transaction {TransactionId}: {NetAmount} to shop {ShopId}",
                    transaction.Id, transaction.NetAmount, shop.Id);
            }

            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing escrow funds");
            return Result.Failure($"Fund release failed: {ex.Message}");
        }
    }
}
