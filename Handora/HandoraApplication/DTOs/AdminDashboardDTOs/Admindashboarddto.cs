using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.AdminDashboardDTOs
{
   // / <summary>
/// The full payload returned to the Admin Dashboard "overview" page.
/// </summary>
public class AdminDashboardDto
    {
        public SalesSummaryDto SalesSummary { get; set; } = new();
        public CountsSummaryDto Counts { get; set; } = new();
        public RevenueChartDto RevenueChart { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = [];
        public List<TopSellerDto> TopSellers { get; set; } = [];
        public List<TopBuyerDto> TopBuyers { get; set; } = [];
        public CouponStatsDto CouponStats { get; set; } = new();
    }
}
