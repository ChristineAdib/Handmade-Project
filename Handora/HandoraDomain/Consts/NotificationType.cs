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
        Coupon = 5
    }
}
