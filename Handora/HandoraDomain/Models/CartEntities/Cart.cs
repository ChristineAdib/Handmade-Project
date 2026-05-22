using HandoraDomain.Models.AppUser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.CartEntities
{
    public class Cart:BaseEntity<Guid>
    {
        //public decimal TotalPrice { get; set; }

        // FK
        public string UserId { get; set; }
        public User User { get; set; } = null!;

        // Navigation
        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}
