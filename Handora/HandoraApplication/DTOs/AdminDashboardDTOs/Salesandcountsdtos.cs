using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.AdminDashboardDTOs
{
    /// <summary>
    /// Total sales amounts (paid orders) over different time windows.
    /// </summary>
    public class SalesSummaryDto
    {
        public decimal TodaySales { get; set; }
        public decimal WeekSales { get; set; }
        public decimal MonthSales { get; set; }
    }

    /// <summary>
    /// "New" entity counters (created today / this week / this month)
    /// plus running totals, used for the top KPI cards.
    /// </summary>
    public class CountsSummaryDto
    {
        public int NewOrdersToday { get; set; }
        public int NewOrdersThisWeek { get; set; }
        public int NewOrdersThisMonth { get; set; }

        public int NewUsersToday { get; set; }
        public int NewUsersThisWeek { get; set; }
        public int NewUsersThisMonth { get; set; }

        public int NewSellersToday { get; set; }
        public int NewSellersThisWeek { get; set; }
        public int NewSellersThisMonth { get; set; }

        public int TotalOrders { get; set; }
        public int TotalUsers { get; set; }
        public int TotalSellers { get; set; }
    }
}
