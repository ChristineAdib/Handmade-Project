using HandoraDomain.Models.ProductEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.WishListEntoties
{
    public class WishListItem:BaseEntity<Guid>
    {
        public int Quantity { get; set; } = 1;

        // FKs
        public Guid WishListId { get; set; }
        public WishList WishList { get; set; } = null!;

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}
