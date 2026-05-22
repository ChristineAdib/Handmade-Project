using HandoraDomain.Models.OrderEntity;
using HandoraInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraInfrastructure.Seeders
{
    public static class DeliveryMethodSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            var methods = new List<DeliveryMethod>
        {
            new("Standard",
                descriptionEn:  "Standard Delivery — delivered to your door",
                descriptionAr:  "شحن عادي — يصلك على بابك",
                deliveryTimeEn: "5-7 Business Days",
                deliveryTimeAr: "من 5 إلى 7 أيام عمل",
                cost: 30.00m)
            {
                Id = Guid.Parse("dddddddd-0000-0000-0000-000000000001"),
                IsActive = true,
            },

            new("Express",
                descriptionEn:  "Express Delivery — faster shipping for urgent orders",
                descriptionAr:  "شحن سريع — للطلبات العاجلة",
                deliveryTimeEn: "2-3 Business Days",
                deliveryTimeAr: "من 2 إلى 3 أيام عمل",
                cost: 60.00m)
            {
                Id = Guid.Parse("dddddddd-0000-0000-0000-000000000002"),
                IsActive = true,
            },

            new("Next Day",
                descriptionEn:  "Next Day Delivery — order today, receive tomorrow",
                descriptionAr:  "توصيل في اليوم التالي — اطلب اليوم واستلم غدًا",
                deliveryTimeEn: "Next Business Day",
                deliveryTimeAr: "يوم العمل التالي",
                cost: 100.00m)
            {
                Id = Guid.Parse("dddddddd-0000-0000-0000-000000000003"),
                IsActive = true,
            },

            new("Store Pickup",
                descriptionEn:  "Pick up your order from the seller directly",
                descriptionAr:  "استلم طلبك من البائع مباشرة",
                deliveryTimeEn: "Same Day",
                deliveryTimeAr: "نفس اليوم",
                cost: 0.00m)
            {
                Id = Guid.Parse("dddddddd-0000-0000-0000-000000000004"),
                IsActive = true,
            },
        };

            var existingShortNames = await context.DeliveryMethods.Select(dm => dm.ShortName).ToListAsync();
            var newMethods = methods.Where(m => !existingShortNames.Contains(m.ShortName)).ToList();

            if (newMethods.Count != 0)
            {
                await context.DeliveryMethods.AddRangeAsync(newMethods);
                await context.SaveChangesAsync();
            }
        }
    }
}
