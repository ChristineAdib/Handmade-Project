namespace HandoraDomain.Models.ProductEntities
{
    public class ProductDraft : BaseEntity<Guid>
    {
        // FK to the live product
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // Draft status
        public DraftStatus Status { get; set; } = DraftStatus.PendingReview;

        // Editable fields (null = no change proposed)
        public string? TitleEn { get; set; }
        public string? TitleAr { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public decimal? Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int? Quantity { get; set; }
        public Guid? CategoryId { get; set; }

        // Serialized tag & image changes (JSON strings)
        public string? ProposedTagsJson { get; set; }       // List<string> serialized
        public string? NewImageUrlsJson { get; set; }        // List<string> serialized (already-uploaded URLs)
        public string? RemoveImageIdsJson { get; set; }      // List<Guid> serialized
    }
}
