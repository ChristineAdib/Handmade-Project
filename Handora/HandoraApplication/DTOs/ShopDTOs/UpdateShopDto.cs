using Microsoft.AspNetCore.Http;

namespace HandoraApplication.DTOs.ShopDTOs
{
    public class UpdateShopDto
    {
        public string? Name { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public IFormFile? Logo { get; set; }
    }
}