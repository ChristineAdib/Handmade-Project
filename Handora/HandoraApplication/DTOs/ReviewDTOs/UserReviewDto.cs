using System;

namespace HandoraApplication.DTOs.ReviewDTOs
{
    public class UserReviewDto
    {
        public Guid Id { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public Guid ProductId { get; set; }
        public string ProductTitle { get; set; } = string.Empty;
        public string? ProductImage { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsVerifiedPurchase { get; set; }
    }
}
