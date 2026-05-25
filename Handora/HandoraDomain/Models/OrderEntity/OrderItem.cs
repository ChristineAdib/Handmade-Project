using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HandoraDomain.Models.ShopEntities;

namespace HandoraDomain.Models.OrderEntity
{
    public class OrderItem : BaseEntity<Guid>
    {
        public OrderItem() { }
        public OrderItem(ProductItemOrdered product, int quantity, decimal price)
        {
            Product = product;
            Quantity = quantity;
            Price = price;
        }

        public ProductItemOrdered Product { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }

        public Guid OrderId { get; set; }
        public Order Order { get; set; }

        public Guid ShopId { get; set; }

        public Shop Shop { get; set; } = null!;
    }
}
