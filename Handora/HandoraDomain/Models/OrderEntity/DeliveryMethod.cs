using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.OrderEntity
{
    public class DeliveryMethod:BaseEntity<Guid>
    {
        public DeliveryMethod() { }

        public DeliveryMethod(string shortName, string descriptionEn, string descriptionAr,
                              string deliveryTimeEn, string deliveryTimeAr, decimal cost)
        {
            ShortName = shortName;
            DescriptionEn = descriptionEn;
            DescriptionAr = descriptionAr;
            DeliveryTimeEn = deliveryTimeEn;
            DeliveryTimeAr = deliveryTimeAr;
            Cost = cost;
        }

        public string ShortName { get; set; }

        public string DescriptionEn { get; set; } = string.Empty;
        public string DescriptionAr { get; set; } = string.Empty;
        public string DeliveryTimeEn { get; set; } = string.Empty;
        public string DeliveryTimeAr { get; set; } = string.Empty;

        public decimal Cost { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
