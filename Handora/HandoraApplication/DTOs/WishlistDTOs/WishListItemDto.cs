using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.WishlistDTOs
{
   public class WishListItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string TitleEn { get; set; } = string.Empty;
        public string TitleAr { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }

        public bool IsAvailable { get; set; }
        public int StockQuantity { get; set; }
        public bool IsSoldOut { get; set; }
    }
}
