using HandoraDomain.Interfaces;
using HandoraDomain.Models.CartEntities;
using HandoraDomain.Models.CouponEntities;
using HandoraDomain.Models.OrderEntity;
using HandoraInfrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HandoraInfrastructure.Repositries;

public class OrderRepository(AppDbContext context)
    : GenericRepository<Order, Guid>(context), IOrderRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Order?> GetOrderByIdWithDetailsAsync(Guid orderId)
    {
        return await _context.Orders
            .Include(o => o.DeliveryMethod)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Items)
                .ThenInclude(i => i.Shop)
                    .ThenInclude(s => s.Owner)
            .Include(o => o.Coupon)
            .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);
    }

    public async Task<IQueryable<Order>> GetOrdersByUserIdQueryAsync(string userId)
    {
        return _context.Orders
            .AsNoTracking()
            .Include(o => o.DeliveryMethod)
            .Include(o => o.Items)
            .Where(o => o.UserId == userId && !o.IsDeleted);
    }

    public async Task<Order?> GetOrderWithItemsAsync(Guid orderId)
    {
        return await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);
    }

    public async Task<Cart?> GetUserCartWithItemsAsync(string userId)
    {
        return await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
                    .ThenInclude(p => p.Images)
            .Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
                    .ThenInclude(p => p.Shop)
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);
    }

    public async Task<Coupon?> GetActiveCouponByCodeAsync(string couponCode)
    {
        return await _context.Coupons
            .FirstOrDefaultAsync(c =>
                c.Code == couponCode && c.IsActive && !c.IsDeleted && c.ExpiryDate > DateTime.UtcNow);
    }

    public async Task<bool> HasUserUsedCouponAsync(string userId, Guid couponId)
    {
        return await _context.Orders
            .AnyAsync(o => o.UserId == userId && o.CouponId == couponId && o.Status != OrderStatus.Cancelled);
    }

    public async Task<IQueryable<Order>> GetOrdersByShopIdQueryAsync(Guid shopId)
    {
        return _context.Orders
            .AsNoTracking()
            .Include(o => o.DeliveryMethod)
            .Include(o => o.Items)
            .Where(o => o.Items.Any(i => i.ShopId == shopId) && !o.IsDeleted);
    }
    public async Task<IQueryable<Order>> GetAllOrdersQueryAsync()
    {
        return _context.Orders
            .AsNoTracking()
            .Include(o => o.DeliveryMethod)
            .Include(o => o.Items)
            .Where(o => !o.IsDeleted);
    }
}
