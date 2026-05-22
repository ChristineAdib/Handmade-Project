using HandoraDomain.Models.AppUser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.ProductEntities
{
    public class Review:BaseEntity<Guid>
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsApproved { get; set; } = true;    // [IMPROVEMENT] allow shops/admins to moderate reviews

        // FKs
        public string UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}
