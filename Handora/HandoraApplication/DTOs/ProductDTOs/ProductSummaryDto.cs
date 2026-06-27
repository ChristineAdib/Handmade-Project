
using HandoraDomain.Models.ProductEntities;

using HandoraDomain.Models.ProductEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.ProductDTOs
{
    public class ProductSummaryDto //for catalog and cards
    {
        public Guid Id { get; set; }
        public string TitleEn { get; set; } = string.Empty;
        public string TitleAr { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal FinalPrice => DiscountPrice ?? Price;
        public string? MainImageUrl { get; set; }            // single thumbnail
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public string CategoryNameEn { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public ProductStatus Status { get; set; }
        public bool IsActive { get; set; }
        public int Quantity { get; set; }
        public bool IsOnePiece { get; set; }
        public string? ArModelUrl { get; set; }
    }
}
