using HandoraDomain.Models.CartEntities;
using HandoraDomain.Models.OrderEntity;
using HandoraDomain.Models.ShopEntities;
using HandoraDomain.Models.WishListEntoties;


namespace HandoraDomain.Models.ProductEntities
{
    public class Product:BaseEntity<Guid>
    {
        // [LOCALIZATION] bilingual title & description
        public string TitleEn { get; set; } = string.Empty;
        public string TitleAr { get; set; } = string.Empty;
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }

        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }     // [IMPROVEMENT] sale price without needing a separate entity
        public int Quantity { get; set; }
        public bool IsOnePiece { get; set; } = false;
        public ProductStatus Status { get; set; } = ProductStatus.Active;
        public bool IsActive { get; set; } = false;
        public decimal AverageRating { get; set; } = 0; // [IMPROVEMENT] denormalized for fast sorting/filtering
        public int ReviewCount { get; set; } = 0;       // [IMPROVEMENT] same reason

        // FKs
        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public Guid ShopId { get; set; }
        public Shop Shop { get; set; } = null!;

        // Navigation Properties
        public ProductDraft? PendingDraft { get; set; }
        public ProductReviewSummary? ReviewSummary { get; set; }
        public ICollection<ProductImage> Images { get; set; } = [];
        public ICollection<Review> Reviews { get; set; } = [];
        public ICollection<Tag> Tags { get; set; } = [];
        public ICollection<OrderItem> OrderItems { get; set; } = [];
        public ICollection<CartItem> CartItems { get; set; } = [];
        public ICollection<WishListItem> WishListItems { get; set; } = [];

        // 3D / AR Model URL
        public string? ArModelUrl { get; set; }
    }
}
