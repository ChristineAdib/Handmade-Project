namespace HandoraApplication.DTOs.ShopDTOs
{
    public class CreateShopDto
    {
        public string Name { get; set; } = string.Empty;
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public string? Logo { get; set; }
    }
}