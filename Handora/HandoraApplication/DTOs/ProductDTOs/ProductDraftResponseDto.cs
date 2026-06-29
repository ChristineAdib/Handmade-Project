namespace HandoraApplication.DTOs.ProductDTOs
{
    public class ProductDraftResponseDto
    {
        public Guid DraftId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Proposed values (null = no change)
        public string? TitleEn { get; set; }
        public string? TitleAr { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public decimal? Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int? Quantity { get; set; }
        public Guid? CategoryId { get; set; }

        public List<string>? ProposedTags { get; set; }
        public List<string>? NewImageUrls { get; set; }
        public List<Guid>? RemoveImageIds { get; set; }
        public string? ArModelUrl { get; set; }
        public bool? RemoveArModel { get; set; }
    }
}
