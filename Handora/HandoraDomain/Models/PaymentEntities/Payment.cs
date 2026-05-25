using HandoraDomain.Models.OrderEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.PaymentEntities
{
    public class Payment : BaseEntity<Guid>
    {

        public DateTime? PaidAt { get; set; }
        public string? Currency { get; set; }

        public string? RawResponse { get; set; }

        public string? Provider { get; set; }          // "Paymob"
        public string? ProviderOrderId { get; set; }
        public string? ProviderTransactionId { get; set; }







        public PaymentMethod Method { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string? TransactionId { get; set; }

        public Guid OrderId { get; set; }
        public Order Order { get; set; } = null!;
    }
}
