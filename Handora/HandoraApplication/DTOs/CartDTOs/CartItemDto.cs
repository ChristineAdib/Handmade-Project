using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.CartDTOs
{
    public class CartItemDto
    {
        public Guid ProductId { get; set; }
        public string TitleEn { get; set; } = string.Empty;
        public string TitleAr { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => (DiscountPrice ?? Price) * Quantity;
    }
}
