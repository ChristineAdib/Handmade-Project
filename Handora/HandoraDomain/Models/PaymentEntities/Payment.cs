using HandoraDomain.Models.OrderEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.PaymentEntities
{
    public class Payment:BaseEntity<Guid>
    {
        public PaymentMethod Method { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string? TransactionId { get; set; }
        public DateTime? PaidAt { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
    }
}
