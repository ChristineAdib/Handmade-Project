namespace HandoraApplication.DTOs.ShopDTOs
{
    public class ShopStatsDto
    {
        public decimal TotalSales { get; set; }
        public decimal Rating { get; set; }
        public int ReviewCount { get; set; }
        public int ProductCount { get; set; }
        public int ActiveProductCount { get; set; }
    }
}