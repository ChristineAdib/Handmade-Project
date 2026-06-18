using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.AdminDashboardDTOs
{

    /// <summary>
    /// One of the Top-5 best selling products.
    /// </summary>
    public class TopProductDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? PictureUrl { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    /// <summary>
    /// A seller (shop owner) ranked by activity / revenue generated.
    /// </summary>
    public class TopSellerDto
    {
        public Guid ShopId { get; set; }
        public string SellerId { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public string? Logo { get; set; }
        public int OrdersCount { get; set; }
        public int ProductsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal Rating { get; set; }
    }

    /// <summary>
    /// A buyer ranked by activity / amount spent, including how much they
    /// relied on coupons.
    /// </summary>
    public class TopBuyerDto
    {
        public string BuyerId { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int OrdersCount { get; set; }
        public decimal TotalSpent { get; set; }
        public int CouponsUsedCount { get; set; }
        public decimal TotalDiscountReceived { get; set; }
    }

    /// <summary>
    /// One coupon ranked by how often it was redeemed.
    /// </summary>
    public class TopCouponDto
    {
        public Guid CouponId { get; set; }
        public string Code { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public decimal TotalDiscountGiven { get; set; }
        public bool IsActive { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

    /// <summary>
    /// Aggregate coupon usage statistics, used to enrich the
    /// "Most Active Buyers" widget with coupon-related insights.
    /// </summary>
    public class CouponStatsDto
    {
        public int ActiveCouponsCount { get; set; }
        public int TotalCouponsUsedCount { get; set; }
        public decimal TotalDiscountGiven { get; set; }
        public List<TopCouponDto> TopCoupons { get; set; } = [];
    }

}
