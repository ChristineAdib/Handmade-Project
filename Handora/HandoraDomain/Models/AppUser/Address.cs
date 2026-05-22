using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.AppUser
{
    public class Address:BaseEntity<Guid>
    {
        public string AddressLine { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? PostalCode { get; set; }

        // FK
        public string UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
