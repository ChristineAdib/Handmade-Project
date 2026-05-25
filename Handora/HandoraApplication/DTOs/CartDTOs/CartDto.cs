using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.CartDTOs
{
    public class CartDto
    {
        public string CartId { get; set; } = string.Empty;
        public List<CartItemDto> Items { get; set; } = [];
        public int TotalItems => Items.Sum(i => i.Quantity);
        public decimal TotalPrice => Items.Sum(i => i.TotalPrice);
    }
}
