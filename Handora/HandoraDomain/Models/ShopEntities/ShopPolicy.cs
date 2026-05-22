using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.ShopEntities
{
    public class ShopPolicy:BaseEntity<Guid>
    {
        public string Content { get; set; } = string.Empty;

        // FK
        public Guid ShopId { get; set; }
        public Shop Shop { get; set; } = null!;
    }
}
