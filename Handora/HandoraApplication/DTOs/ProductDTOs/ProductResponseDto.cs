using HandoraApplication.DTOs.ReviewDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.ProductDTOs
{
    public class ProductResponseDto
    {
        public Guid Id { get; set; }

        public string TitleEn { get; set; }
        public string TitleAr { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }

        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal FinalPrice => DiscountPrice ?? Price; // calculated

        public int Quantity { get; set; }
        public string Status { get; set; } // "Active", "Inactive", "OutOfStock"
        public bool IsActive { get; set; }

        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }

        public Guid CategoryId { get; set; }
        public Guid? ParentCategoryId { get; set; }
        public string CategoryNameEn { get; set; }
        public string CategoryNameAr { get; set; }

        public Guid ShopId { get; set; }
        public string ShopName { get; set; }

        public List<ProductImageDto> Images { get; set; } = [];

        public List<string> Tags { get; set; } = [];


        public List<ReviewSummaryDto> LatestReviews { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ProductDraftResponseDto? PendingDraft { get; set; }
        public bool HasPendingDraft => PendingDraft != null;


        public bool IsAvailable { get; set; }
        public int StockQuantity { get; set; }
        public bool IsSoldOut { get; set; }
    }
}
