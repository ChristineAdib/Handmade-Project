using System;

namespace HandoraApplication.DTOs.ShopReviewDTOs
{
    public class CreateShopReviewDto
    {
        public Guid ShopId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
