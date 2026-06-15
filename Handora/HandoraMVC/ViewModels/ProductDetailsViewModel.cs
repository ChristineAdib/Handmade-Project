using HandoraApplication.DTOs.ProductDTOs;
using System;
using System.Collections.Generic;

namespace HandoraMVC.ViewModels
{
    public class ProductDetailsViewModel
    {
        public Guid Id { get; set; }
        public string TitleEn { get; set; } = string.Empty;
        public string TitleAr { get; set; } = string.Empty;
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal FinalPrice => DiscountPrice ?? Price;
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryNameEn { get; set; } = string.Empty;
        public string CategoryNameAr { get; set; } = string.Empty;
        public Guid ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public List<ProductImageDto> Images { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ProductDraftResponseDto? PendingDraft { get; set; }
        public bool HasPendingDraft => PendingDraft != null;
    }
}
