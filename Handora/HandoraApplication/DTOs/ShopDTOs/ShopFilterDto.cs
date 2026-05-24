namespace HandoraApplication.DTOs.ShopDTOs
{
    public class ShopFilterDto
    {
        public string? Search { get; set; }
        public decimal? MinRating { get; set; }
        public bool? IsVerified { get; set; }
        public string? SortBy { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}