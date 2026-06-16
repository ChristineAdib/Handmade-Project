using System;

namespace HandoraApplication.DTOs.ReviewDTOs
{
    public class ReviewEligibilityDto
    {
        public bool IsEligible { get; set; }
        public bool AlreadyReviewed { get; set; }
        public Guid? ExistingReviewId { get; set; }
        public int ExistingRating { get; set; }
        public string? ExistingComment { get; set; }
    }
}
