using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.WishlistDTOs
{
   public class WishListDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public List<WishListItemDto> Items { get; set; } = [];
        public int TotalItems => Items.Count;
    }
}
