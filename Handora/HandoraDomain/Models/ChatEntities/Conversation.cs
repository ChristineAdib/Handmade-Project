using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.CustomStudioEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.ChatEntities
{
    public class Conversation : BaseEntity<Guid>
    {
        public string BuyerId { get; set; } = string.Empty;
        public User Buyer { get; set; } = null!;

        public string SellerId { get; set; } = string.Empty;
        public User Seller { get; set; } = null!;

        public Guid? ActiveDesignRequestId { get; set; }
        public CustomRequest? ActiveDesignRequest { get; set; }

        public ICollection<CustomRequest> DesignRequests { get; set; } = new List<CustomRequest>();

        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
