using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.ProductEntities
{
    public class ProductImage
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsMain { get; set; } = false;

        // FK
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}
