using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.NotificationEntities
{
    public enum NotificationType
    {
        Order = 1,
        Payment = 2,
        Review = 3,
        System = 4,
        Coupon = 5,
        Follow = 6,
        Message = 7,
        NewFollower = 8,
        ProductSubmitted = 9,
        ProductApproved = 10,
        ProductRejected = 11,
        ProductUpdated = 12,
        ProductUpdateApproved = 13,
        ProductUpdateRejected = 14,
        NewProductFromFollowedShop = 15,
        NewOrder = 16,
        PaymentReceived = 17,
        UserBanned = 18,
        OrderStatusChanged = 19
    }
}
