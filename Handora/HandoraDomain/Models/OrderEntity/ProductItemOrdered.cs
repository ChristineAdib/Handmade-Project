using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.OrderEntity
{
    public class ProductItemOrdered:BaseEntity<Guid>
    {
        public ProductItemOrdered()
        {
            
        }
        public ProductItemOrdered(Guid productId, string productName, string pictureUrl)
        {
            ProductId = productId;
            ProductName = productName;
            PictureUrl = pictureUrl;
        }

        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string PictureUrl { get; set; }

    }
}
