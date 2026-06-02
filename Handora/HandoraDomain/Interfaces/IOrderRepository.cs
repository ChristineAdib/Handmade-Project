using HandoraDomain.Models.CartEntities;
using HandoraDomain.Models.CouponEntities;
using HandoraDomain.Models.OrderEntity;

namespace HandoraDomain.Interfaces;

public interface IOrderRepository : IGenericRepository<Order, Guid>
{
    Task<Order?> GetOrderByIdWithDetailsAsync(Guid orderId);
    Task<IQueryable<Order>> GetOrdersByUserIdQueryAsync(string userId);
    Task<Order?> GetOrderWithItemsAsync(Guid orderId);
    Task<Cart?> GetUserCartWithItemsAsync(string userId);
    Task<Coupon?> GetActiveCouponByCodeAsync(string couponCode);
    Task<bool> HasUserUsedCouponAsync(string userId, Guid couponId);
}
