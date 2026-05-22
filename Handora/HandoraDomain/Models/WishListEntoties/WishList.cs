using HandoraDomain.Models.AppUser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.WishListEntoties
{
    public class WishList:BaseEntity<Guid>
    {
        //public decimal TotalPrice { get; set; }

        // FK
        public string UserId { get; set; }
        public User User { get; set; } = null!;

        // Navigation
        public ICollection<WishListItem> Items { get; set; } = [];
    }
}
