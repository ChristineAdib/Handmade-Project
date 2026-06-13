using HandoraApplication.DTOs.Common;
using HandoraApplication.DTOs.OrderDTOs;
using HandoraApplication.Helpers;

namespace HandoraApplication.IServices;

public interface IOrderService
{
    Task<Result<OrderResponseDto>> CreateOrder(string userId, string buyerEmail, CreateOrderDto dto);
    Task<Result<OrderResponseDto>> GetOrderById(Guid orderId, string userId, bool isAdmin);
    Task<Result<PagedResultDto<OrderSummaryDto>>> GetUserOrders(string userId, OrderQueryDto query);
    Task<Result<OrderResponseDto>> UpdateOrderStatus(Guid orderId, UpdateOrderStatusDto dto, string userId, bool isAdmin);
    Task<Result> CancelOrder(Guid orderId, string userId);
    Task<Result<PagedResultDto<OrderSummaryDto>>> GetSellerOrders(Guid shopId, OrderQueryDto query);
    Task<Result<PagedResultDto<OrderSummaryDto>>> GetAllOrders(OrderQueryDto query);
    Task<Result<OrderResponseDto>> GetOrderByIdForAdmin(Guid orderId);
}
