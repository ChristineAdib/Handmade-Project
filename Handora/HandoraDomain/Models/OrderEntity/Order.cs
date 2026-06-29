using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.CouponEntities;
using HandoraDomain.Models.PaymentEntities;
using HandoraDomain.Models.CustomStudioEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.OrderEntity
{
    public class Order : BaseEntity<Guid>
    {
        public Order()
        {

        }
        public Order(
    string buyerEmail,
    OrderShippingAddress shippingAddress,
    DeliveryMethod deliveryMethod,
    ICollection<OrderItem> items,
    decimal subTotal,
    string? paymentToken)
        {
            BuyerEmail = buyerEmail;
            ShippingAddress = shippingAddress;
            DeliveryMethod = deliveryMethod;
            Items = items;
            SubTotal = subTotal;
            PaymentIntentId = paymentToken;
            OrderDate = DateTime.UtcNow;
        }
        // Paymob fields
        public string? PaymentIntentId { get; set; }         // Paymob payment token (iframe key)
        public string? PaymobOrderId { get; set; }           // Paymob order ID (for webhook)
        public bool IsFundsReleased { get; set; } = false;  // whether 14-day hold is released
        public DateTime? DeliveredAt { get; set; }          // timestamp when order was marked delivered
        public decimal TotalAmount { get; set; }
        public decimal SellerAmount { get; set; }
        public decimal PlatformCommission { get; set; }



        public string BuyerEmail { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public OrderShippingAddress ShippingAddress { get; set; }
        public Guid DeliveryMethodId { get; set; }
        public DeliveryMethod DeliveryMethod { get; set; }
        public ICollection<OrderItem> Items { get; set; } = new HashSet<OrderItem>();
        public decimal SubTotal { get; set; }
        //[NotMapped]
        //public decimal Total { get => SubTotal + DeliveryMethod.Cost; }
        public Guid? CouponId { get; set; }
        public Coupon? Coupon { get; set; }
        public decimal? DiscountAmount { get; set; }    // [IMPROVEMENT] track how much the coupon saved
        public string? Notes { get; set; }              // [IMPROVEMENT] buyer can add delivery notes

        public string UserId { get; set; }
        public User User { get; set; } = null!;

        public decimal GetTotal()
            => SubTotal + DeliveryMethod.Cost;

        public Payment? Payment { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        public Guid? CustomOfferId { get; set; }
        public CustomOffer? CustomOffer { get; set; }
    }
}
