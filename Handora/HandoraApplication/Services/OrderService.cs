using HandoraApplication.DTOs.Common;
using HandoraApplication.DTOs.OrderDTOs;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.CartEntities;
using HandoraDomain.Models.ShopEntities;
using HandoraDomain.Models.OrderEntity;
using HandoraDomain.Models.ProductEntities;
using Microsoft.EntityFrameworkCore;
using HandoraDomain.Models.NotificationEntities;
using HandoraApplication.DTOs.NotificationsDto;
using HandoraDomain.Models.CouponEntities;

namespace HandoraApplication.Services;

public class OrderService(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    IEscrowService escrowService,
    INotificationService notificationService,
    IAuthRepository authRepository) : IOrderService
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IEscrowService _escrowService = escrowService;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IAuthRepository _authRepository = authRepository;

    public async Task<Result<OrderResponseDto>> CreateOrder(string userId, string buyerEmail, CreateOrderDto dto)
    {
        // 1. Get user's cart with items
        var cart = await _orderRepository.GetUserCartWithItemsAsync(userId);

        if (cart is null || !cart.Items.Any())
            return Result<OrderResponseDto>.Failure("Cart is empty or not found");

        if (cart.Items.Any(ci => ci.Product.Quantity <= 0 || ci.Product.IsDeleted || ci.Product.Status != ProductStatus.Active))
        {
            return Result<OrderResponseDto>.Failure("One or more products in your cart are currently unavailable.");
        }

        // 2. Validate delivery method
        var deliveryRepo = _unitOfWork.Repository<DeliveryMethod, Guid>();
        var deliveryMethod = await deliveryRepo.GetByIdAsync(dto.DeliveryMethodId);

        if (deliveryMethod is null || !deliveryMethod.IsActive)
            return Result<OrderResponseDto>.Failure("Invalid or inactive delivery method");

        // 3. Build order items from cart & calculate subtotal
        var orderItems = new List<OrderItem>();
        decimal subTotal = 0;

        foreach (var cartItem in cart.Items)
        {
            var product = cartItem.Product;

            if (product.IsDeleted || product.Status != ProductStatus.Active)
                return Result<OrderResponseDto>.Failure($"Product '{product.TitleEn}' is no longer available");

            if (product.Shop.OwnerId == userId)
                return Result<OrderResponseDto>.Failure("You cannot purchase your own products.");

            if (product.Quantity < cartItem.Quantity)
                return Result<OrderResponseDto>.Failure($"Insufficient stock for '{product.TitleEn}'. Available: {product.Quantity}");

            var unitPrice = product.DiscountPrice ?? product.Price;
            var mainImage = product.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl
                         ?? product.Images.FirstOrDefault()?.ImageUrl
                         ?? string.Empty;

            var productItemOrdered = new ProductItemOrdered(product.Id, product.TitleEn, mainImage);

            var orderItem = new OrderItem(productItemOrdered, cartItem.Quantity, unitPrice)
            {
                Id = Guid.NewGuid(),
                ShopId = product.ShopId
            };

            orderItems.Add(orderItem);
            subTotal += unitPrice * cartItem.Quantity;
        }

        // 4. Handle coupon (optional)
        Coupon? coupon = null;
        decimal discountAmount = 0;

        if (!string.IsNullOrWhiteSpace(dto.CouponCode))
        {
            var normalizedCode = dto.CouponCode.Trim().ToUpper();
            coupon = await _orderRepository.GetActiveCouponByCodeAsync(normalizedCode);

            if (coupon is null)
                return Result<OrderResponseDto>.Failure("Invalid, inactive, or expired coupon");

            if (coupon.MaxUsageCount.HasValue && coupon.CurrentUsageCount >= coupon.MaxUsageCount.Value)
                return Result<OrderResponseDto>.Failure("Coupon usage limit has been reached");

            var alreadyUsed = await _orderRepository.HasUserUsedCouponAsync(userId, coupon.Id);
            if (alreadyUsed)
                return Result<OrderResponseDto>.Failure("You have already redeemed this coupon");

            var shopIds = orderItems.Select(i => i.ShopId).Distinct().ToList();
            var shopRepo = _unitOfWork.Repository<Shop, Guid>();
            var shopsQuery = await shopRepo.GetAllAsNoTracking();
            var shops = await shopsQuery
                .Where(s => shopIds.Contains(s.Id) && !s.IsDeleted)
                .ToListAsync();

            var sellerItems = orderItems.Where(item => {
                var shop = shops.FirstOrDefault(s => s.Id == item.ShopId);
                return shop != null && shop.OwnerId == coupon.SellerId;
            }).ToList();

            if (!sellerItems.Any())
                return Result<OrderResponseDto>.Failure("This coupon is not valid for any items in your cart");

            decimal shopSubtotal = sellerItems.Sum(i => i.Price * i.Quantity);

            if (coupon.MinOrderValue.HasValue && shopSubtotal < coupon.MinOrderValue.Value)
                return Result<OrderResponseDto>.Failure($"This coupon requires a minimum subtotal of {coupon.MinOrderValue.Value:C} for products from this store");

            if (coupon.DiscountType == DiscountType.Percentage)
            {
                discountAmount = shopSubtotal * (coupon.DiscountValue / 100m);
            }
            else if (coupon.DiscountType == DiscountType.FixedAmount)
            {
                discountAmount = coupon.DiscountValue;
                if (discountAmount > shopSubtotal)
                {
                    discountAmount = shopSubtotal;
                }
            }

            discountAmount = Math.Round(discountAmount, 2);

            coupon.CurrentUsageCount++;
            var couponRepo = _unitOfWork.Repository<Coupon, Guid>();
            await couponRepo.UpdateAsync(coupon);
        }

        // 5. Create Order
        var shippingAddress = new OrderShippingAddress(dto.FirstName, dto.LastName, dto.Street, dto.City, dto.Country);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            BuyerEmail = buyerEmail,
            ShippingAddress = shippingAddress,
            DeliveryMethodId = dto.DeliveryMethodId,
            Items = orderItems,
            SubTotal = subTotal,
            CouponId = coupon?.Id,
            DiscountAmount = discountAmount > 0 ? discountAmount : null,
            Notes = dto.Notes,
            UserId = userId,
            Status = OrderStatus.Pending
        };

        var deliveryCost = deliveryMethod.Cost;
        order.TotalAmount = subTotal + deliveryCost - discountAmount;
        order.PlatformCommission = order.TotalAmount * 0.10m;
        order.SellerAmount = order.TotalAmount - order.PlatformCommission;

        // 6. Deduct product stock
        var productRepo = _unitOfWork.Repository<Product, Guid>();
        foreach (var cartItem in cart.Items)
        {
            cartItem.Product.Quantity -= cartItem.Quantity;
            await productRepo.UpdateAsync(cartItem.Product);
        }

        // 7. Save order
        await _orderRepository.AddAsync(order);

        // 8. Clear cart items
        var cartItemRepo = _unitOfWork.Repository<CartItem, Guid>();
        foreach (var item in cart.Items.ToList())
        {
            await cartItemRepo.HardDeleteAsync(item);
        }

        await _unitOfWork.SaveChangesAsync();

        return await GetOrderById(order.Id, userId, false);
    }

    public async Task<Result<OrderResponseDto>> GetOrderById(Guid orderId, string userId, bool isAdmin)
    {
        var order = await _orderRepository.GetOrderByIdWithDetailsAsync(orderId);

        if (order is null)
            return Result<OrderResponseDto>.Failure("Order not found");

        if (!isAdmin && order.UserId != userId)
        {
            var shopRepo = _unitOfWork.Repository<Shop, Guid>();
            var shopQuery = await shopRepo.GetAllAsNoTracking();
            var sellerShop = await shopQuery.FirstOrDefaultAsync(s => s.OwnerId == userId && !s.IsDeleted);

            bool isSellerForThisOrder = sellerShop != null && order.Items.Any(i => i.ShopId == sellerShop.Id);

            if (!isSellerForThisOrder)
            {
                return Result<OrderResponseDto>.Failure("You are not authorized to view this order");
            }
        }

        return Result<OrderResponseDto>.Success(MapToResponse(order));
    }

    public async Task<Result<PagedResultDto<OrderSummaryDto>>> GetUserOrders(string userId, OrderQueryDto query)
    {
        var ordersQuery = await _orderRepository.GetOrdersByUserIdQueryAsync(userId);

        // Filter by status
        if (query.Status.HasValue)
            ordersQuery = ordersQuery.Where(o => o.Status == query.Status.Value);

        // Sorting
        ordersQuery = query.SortBy?.ToLower() switch
        {
            "date" => query.SortDescending
                ? ordersQuery.OrderByDescending(o => o.OrderDate)
                : ordersQuery.OrderBy(o => o.OrderDate),
            "total" => query.SortDescending
                ? ordersQuery.OrderByDescending(o => o.SubTotal)
                : ordersQuery.OrderBy(o => o.SubTotal),
            _ => ordersQuery.OrderByDescending(o => o.OrderDate)
        };

        var totalCount = await ordersQuery.CountAsync();

        var items = await ordersQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var summaries = items.Select(o => new OrderSummaryDto
        {
            Id = o.Id,
            OrderDate = o.OrderDate,
            Status = o.Status.ToString(),
            PaymentStatus = o.PaymentStatus.ToString(),
            Total = o.SubTotal + o.DeliveryMethod.Cost - (o.DiscountAmount ?? 0),
            ItemCount = o.Items.Count
        }).ToList();

        var result = new PagedResultDto<OrderSummaryDto>
        {
            Items = summaries,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };

        return Result<PagedResultDto<OrderSummaryDto>>.Success(result);
    }

    public async Task<Result<OrderResponseDto>> UpdateOrderStatus(Guid orderId, UpdateOrderStatusDto dto, string userId, bool isAdmin)
    {
        var order = await _orderRepository.GetOrderByIdWithDetailsAsync(orderId);

        if (order is null)
            return Result<OrderResponseDto>.Failure("Order not found");

        if (!isAdmin)
        {
            var shopRepo = _unitOfWork.Repository<Shop, Guid>();
            var shopQuery = await shopRepo.GetAllAsNoTracking();
            var sellerShop = await shopQuery.FirstOrDefaultAsync(s => s.OwnerId == userId && !s.IsDeleted);

            bool isSellerForThisOrder = sellerShop != null && order.Items.Any(i => i.ShopId == sellerShop.Id);

            if (!isSellerForThisOrder)
            {
                return Result<OrderResponseDto>.Failure("You are not authorized to update the status of this order.");
            }

            // Sellers can transition: Pending → Processing, Confirmed → Processing, Processing → Shipped
            var isValidSellerTransition =
                (order.Status == OrderStatus.Pending    && dto.Status == OrderStatus.Processing) ||
                (order.Status == OrderStatus.Confirmed  && dto.Status == OrderStatus.Processing) ||
                (order.Status == OrderStatus.Processing && dto.Status == OrderStatus.Shipped);

            if (!isValidSellerTransition)
            {
                if (order.Status != dto.Status) // If it's a no-op, let it pass
                {
                    return Result<OrderResponseDto>.Failure(
                        "Sellers are only allowed to transition orders: Pending/Confirmed → Processing, or Processing → Shipped.");
                }
            }
        }

        if (order.Status == OrderStatus.Delivered)
        {
            return Result<OrderResponseDto>.Failure("Order is already delivered and cannot be modified.");
        }

        var currentStatus = order.Status;
        var nextStatus = dto.Status;

        if (currentStatus == nextStatus)
        {
            return Result<OrderResponseDto>.Success(MapToResponse(order));
        }

        if (nextStatus == OrderStatus.Cancelled)
        {
            // Restore product stock
            var productRepo = _unitOfWork.Repository<Product, Guid>();
            foreach (var item in order.Items)
            {
                var product = await productRepo.GetByIdAsync(item.Product.ProductId);
                if (product != null)
                {
                    product.Quantity += item.Quantity;
                    await productRepo.UpdateAsync(product);
                }
            }
            order.Status = nextStatus;
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = userId;
            await _orderRepository.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();
        }
        else if (nextStatus == OrderStatus.Delivered)
        {
            var releaseResult = await _escrowService.RecordDeliveryAsync(orderId);
            if (!releaseResult.IsSuccess)
            {
                return Result<OrderResponseDto>.Failure(releaseResult.Errors.FirstOrDefault() ?? "Failed to release funds on delivery");
            }
            // Reload order details to pick up status, DeliveredAt, and other fields set by EscrowService
            order = await _orderRepository.GetOrderByIdWithDetailsAsync(orderId);
            if (order is null)
                return Result<OrderResponseDto>.Failure("Order not found after updating status");
        }
        else
        {
            order.Status = nextStatus;
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = userId;
            await _orderRepository.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();
        }

        // Send Notification (Scenario 10)
        try
        {
            var statusStr = nextStatus.ToString();
            string statusEn = statusStr;
            string statusAr = statusStr;
            switch (nextStatus)
            {
                case OrderStatus.Pending:
                    statusEn = "Pending";
                    statusAr = "معلق";
                    break;
                case OrderStatus.Confirmed:
                    statusEn = "Confirmed";
                    statusAr = "مؤكد";
                    break;
                case OrderStatus.Processing:
                    statusEn = "Processing";
                    statusAr = "قيد التجهيز";
                    break;
                case OrderStatus.Shipped:
                    statusEn = "Shipped";
                    statusAr = "تم الشحن";
                    break;
                case OrderStatus.Delivered:
                    statusEn = "Delivered";
                    statusAr = "تم التوصيل";
                    break;
                case OrderStatus.Cancelled:
                    statusEn = "Cancelled";
                    statusAr = "ملغي";
                    break;
                case OrderStatus.Refunded:
                    statusEn = "Refunded";
                    statusAr = "تم الاسترجاع";
                    break;
            }

            // Notify the buyer
            await _notificationService.SendAsync(new SendNotificationDto
            {
                UserId = order.UserId,
                TitleEn = "Order Status Updated",
                TitleAr = "تحديث حالة الطلب",
                MessageEn = $"Your order {order.Id} status has changed to {statusEn}.",
                MessageAr = $"تغيرت حالة طلبك {order.Id} إلى {statusAr}.",
                Type = NotificationType.OrderStatusChanged,
                ReferenceId = order.Id,
                ReferenceType = "Order"
            });

            // Notify all admins when a seller updates the order status
            if (!isAdmin)
            {
                var admins = await _authRepository.GetUsersInRoleAsync(AppRoles.Admin);
                foreach (var admin in admins)
                {
                    await _notificationService.SendAsync(new SendNotificationDto
                    {
                        UserId = admin.Id,
                        TitleEn = "Seller Updated Order Status",
                        TitleAr = "السيلر غيّر حالة الطلب",
                        MessageEn = $"Order {order.Id} status was changed to {statusEn} by the seller.",
                        MessageAr = $"قام السيلر بتغيير حالة الطلب {order.Id} إلى {statusAr}.",
                        Type = NotificationType.OrderStatusChanged,
                        ReferenceId = order.Id,
                        ReferenceType = "Order"
                    });
                }
            }
        }
        catch (System.Exception)
        {
            // Ignore
        }

        return Result<OrderResponseDto>.Success(MapToResponse(order));
    }

    public async Task<Result> CancelOrder(Guid orderId, string userId)
    {
        var order = await _orderRepository.GetOrderWithItemsAsync(orderId);

        if (order is null)
            return Result.Failure("Order not found");

        if (order.UserId != userId)
            return Result.Failure("You are not authorized to cancel this order");

        if (order.Status == OrderStatus.Delivered)
            return Result.Failure("Order is already delivered and cannot be modified.");

        if (order.Status != OrderStatus.Pending)
            return Result.Failure("Only pending orders can be cancelled");

        // Restore product stock
        var productRepo = _unitOfWork.Repository<Product, Guid>();
        foreach (var item in order.Items)
        {
            var product = await productRepo.GetByIdAsync(item.Product.ProductId);
            if (product != null)
            {
                product.Quantity += item.Quantity;
                await productRepo.UpdateAsync(product);
            }
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;

        await _orderRepository.UpdateAsync(order);
        await _unitOfWork.SaveChangesAsync();

        // Send Notification (Scenario 10)
        try
        {
            await _notificationService.SendAsync(new SendNotificationDto
            {
                UserId = order.UserId,
                TitleEn = "Order Cancelled",
                TitleAr = "تم إلغاء الطلب",
                MessageEn = $"Your order {order.Id} has been cancelled.",
                MessageAr = $"تم إلغاء طلبك {order.Id}.",
                Type = NotificationType.OrderStatusChanged,
                ReferenceId = order.Id,
                ReferenceType = "Order"
            });
        }
        catch (System.Exception)
        {
            // Ignore
        }

        return Result.Success();
    }

    private static OrderResponseDto MapToResponse(Order order)
    {
        return new OrderResponseDto
        {
            Id = order.Id,
            BuyerEmail = order.BuyerEmail,
            OrderDate = order.OrderDate,
            Status = order.Status.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            FirstName = order.ShippingAddress?.FirstName ?? string.Empty,
            LastName = order.ShippingAddress?.LastName ?? string.Empty,
            Street = order.ShippingAddress?.Street ?? string.Empty,
            City = order.ShippingAddress?.City ?? string.Empty,
            Country = order.ShippingAddress?.Country ?? string.Empty,
            DeliveryMethodName = order.DeliveryMethod?.ShortName ?? string.Empty,
            DeliveryMethodCost = order.DeliveryMethod?.Cost ?? 0,
            SubTotal = order.SubTotal,
            DiscountAmount = order.DiscountAmount,
            Total = order.SubTotal + (order.DeliveryMethod?.Cost ?? 0) - (order.DiscountAmount ?? 0),
            Notes = order.Notes,
            CouponCode = order.Coupon?.Code,
            PaymobOrderId = order.PaymobOrderId,
            Items = order.Items.Select(i => new OrderItemResponseDto
            {
                Id = i.Id,
                ProductId = i.Product.ProductId,
                ProductName = i.Product.ProductName,
                PictureUrl = i.Product.PictureUrl,
                Quantity = i.Quantity,
                Price = i.Price,
                Total = i.Price * i.Quantity,
                ShopName = i.Shop?.Name ?? string.Empty,
                SellerName = i.Shop?.Owner?.Name ?? string.Empty,
                SellerEmail = i.Shop?.Owner?.Email ?? string.Empty,
                SellerPhone = i.Shop?.Owner?.PhoneNumber ?? string.Empty
            }).ToList()
        };
    }

    public async Task<Result<PagedResultDto<OrderSummaryDto>>> GetSellerOrders(Guid shopId, OrderQueryDto query)
    {
        var ordersQuery = await _orderRepository.GetOrdersByShopIdQueryAsync(shopId);

        if (query.Status.HasValue)
            ordersQuery = ordersQuery.Where(o => o.Status == query.Status.Value);

        ordersQuery = query.SortBy?.ToLower() switch
        {
            "date" => query.SortDescending
                ? ordersQuery.OrderByDescending(o => o.OrderDate)
                : ordersQuery.OrderBy(o => o.OrderDate),
            _ => ordersQuery.OrderByDescending(o => o.OrderDate)
        };

        var totalCount = await ordersQuery.CountAsync();
        var items = await ordersQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var summaries = items.Select(o => new OrderSummaryDto
        {
            Id = o.Id,
            OrderDate = o.OrderDate,
            Status = o.Status.ToString(),
            PaymentStatus = o.PaymentStatus.ToString(),
            Total = o.SubTotal + o.DeliveryMethod.Cost - (o.DiscountAmount ?? 0),
            ItemCount = o.Items.Count(i => i.ShopId == shopId)
        }).ToList();

        return Result<PagedResultDto<OrderSummaryDto>>.Success(new PagedResultDto<OrderSummaryDto>
        {
            Items = summaries,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        });
    }

    public async Task<Result<PagedResultDto<OrderSummaryDto>>> GetAllOrders(OrderQueryDto query)
    {
        var ordersQuery = await _orderRepository.GetAllOrdersQueryAsync();

        if (query.Status.HasValue)
            ordersQuery = ordersQuery.Where(o => o.Status == query.Status.Value);

        ordersQuery = query.SortBy?.ToLower() switch
        {
            "date" => query.SortDescending
                ? ordersQuery.OrderByDescending(o => o.OrderDate)
                : ordersQuery.OrderBy(o => o.OrderDate),
            "total" => query.SortDescending
                ? ordersQuery.OrderByDescending(o => o.SubTotal)
                : ordersQuery.OrderBy(o => o.SubTotal),
            _ => ordersQuery.OrderByDescending(o => o.OrderDate)
        };

        var totalCount = await ordersQuery.CountAsync();

        var items = await ordersQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var summaries = items.Select(o => new OrderSummaryDto
        {
            Id = o.Id,
            OrderDate = o.OrderDate,
            Status = o.Status.ToString(),
            PaymentStatus = o.PaymentStatus.ToString(),
            Total = o.SubTotal + o.DeliveryMethod.Cost - (o.DiscountAmount ?? 0),
            ItemCount = o.Items.Count
        }).ToList();

        return Result<PagedResultDto<OrderSummaryDto>>.Success(new PagedResultDto<OrderSummaryDto>
        {
            Items = summaries,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        });
    }
    public async Task<Result<OrderResponseDto>> GetOrderByIdForAdmin(Guid orderId)
    {
        var order = await _orderRepository.GetOrderByIdWithDetailsAsync(orderId);

        if (order is null)
            return Result<OrderResponseDto>.Failure("Order not found");

        return Result<OrderResponseDto>.Success(MapToResponse(order));
    }

}
