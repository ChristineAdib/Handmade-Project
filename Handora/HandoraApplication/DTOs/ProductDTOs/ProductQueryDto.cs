using HandoraApplication.DTOs.Common;
using HandoraDomain.Models.ProductEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.ProductDTOs
{
    public class ProductQueryDto : PaginationQueryDto
    {
        public string? Search { get; set; }
        public Guid? CategoryId { get; set; }
        public List<Guid>? CategoryIds { get; set; }
        public Guid? ShopId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public decimal? MinRating { get; set; }
        public ProductStatus? Status { get; set; }
        public bool? IsAdmin { get; set; }
        public string? SortBy { get; set; }          // "price", "rating", "newest"
        public bool SortDescending { get; set; } = false;
        public bool? OnlyOnePiece { get; set; }
        public List<string>? Tags { get; set; }
    }
}
