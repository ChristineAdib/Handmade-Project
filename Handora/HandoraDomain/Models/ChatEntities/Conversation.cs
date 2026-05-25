using HandoraDomain.Models.AppUser;
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

        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
