using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.IServices.AI
{
    public interface IRagService
    {
        // يبحث ويرد بإجابة ذكية
        Task<string> SearchProductsAsync(string userQuestion);

        // يعمل index لمنتج واحد (بيتكلم لما تضيف/تعدل منتج)
        Task IndexProductAsync(string productId, string name, string description,
                               string category, decimal price, string sellerName);

        // يعمل index لكل المنتجات (بيتكلم مرة واحدة في البداية)
        Task IndexAllProductsAsync();
    }
}
