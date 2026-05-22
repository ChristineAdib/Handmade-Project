using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.ProductDTOs
{
    internal class CreateProductDto
    {
        public string TitleEn { get; set; }
        public string TitleAr { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public Guid CategoryId { get; set; }
        public Guid ShopId { get; set; }
        //public List<IFormFile> Images { get; set; } // Multiple images
        public List<string> Tags { get; set; }
    }
}
