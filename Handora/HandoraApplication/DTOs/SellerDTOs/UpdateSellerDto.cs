using Microsoft.AspNetCore.Http;

namespace HandoraApplication.DTOs.SellerDTOs
{
    public class UpdateSellerDto
    {
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public IFormFile? ProfileImage { get; set; }
    }
}