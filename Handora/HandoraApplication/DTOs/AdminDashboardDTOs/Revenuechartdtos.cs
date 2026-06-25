using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.AdminDashboardDTOs
{

    /// <summary>
    /// Granularity for the revenue chart.
    /// </summary>
    public enum RevenueChartPeriod
    {
        /// <summary>Last 30 days, grouped by day.</summary>
        Daily = 1,

        /// <summary>Last 12 weeks, grouped by week (starts on Monday).</summary>
        Weekly = 2,

        /// <summary>Last 12 months, grouped by month.</summary>
        Monthly = 3
    }

    /// <summary>
    /// A single point on the revenue chart.
    /// </summary>
    public class RevenueChartPointDto
    {
        /// <summary>
        /// Start date of the bucket (day / week-start / month-start).
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Display label for the X axis (e.g. "12 Jun", "Week 24", "Jun 2026").
        /// </summary>
        public string Label { get; set; } = string.Empty;

        public decimal Revenue { get; set; }
        public int OrdersCount { get; set; }
    }

    public class RevenueChartDto
    {
        public RevenueChartPeriod Period { get; set; }
        public List<RevenueChartPointDto> Points { get; set; } = [];
    }

}
