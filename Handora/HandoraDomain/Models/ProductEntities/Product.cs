using HandoraDomain.Models.CartEntities;
using HandoraDomain.Models.OrderEntity;
using HandoraDomain.Models.ShopEntities;
using HandoraDomain.Models.WishListEntoties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.ProductEntities
{
    public class Product:BaseEntity<int>
    {
        // [LOCALIZATION] bilingual title & description
        public string TitleEn { get; set; } = string.Empty;
        public string TitleAr { get; set; } = string.Empty;
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }

        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }     // [IMPROVEMENT] sale price without needing a separate entity
        public int Quantity { get; set; }
        public ProductStatus Status { get; set; } = ProductStatus.Active;
        public decimal AverageRating { get; set; } = 0; // [IMPROVEMENT] denormalized for fast sorting/filtering
        public int ReviewCount { get; set; } = 0;       // [IMPROVEMENT] same reason

        // FKs
        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public Guid ShopId { get; set; }
        public Shop Shop { get; set; } = null!;

        // Navigation Properties
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<WishListItem> WishListItems { get; set; } = new List<WishListItem>();
    }
}
