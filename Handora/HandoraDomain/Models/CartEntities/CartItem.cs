using HandoraDomain.Models.ProductEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.CartEntities
{
    public class CartItem:BaseEntity<Guid>
    {
        public int Quantity { get; set; } = 1;
        public decimal TotalPrice { get; set; }

        // FKs
        public Guid CartId { get; set; }
        public Cart Cart { get; set; } = null!;

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}
