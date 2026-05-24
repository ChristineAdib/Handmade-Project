namespace HandoraApplication.DTOs.ShopDTOs
{
    public class ShopDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public string? Logo { get; set; }
        public decimal Rating { get; set; }
        public int ReviewCount { get; set; }
        public decimal TotalSales { get; set; }
        public bool IsVerified { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
    }
}