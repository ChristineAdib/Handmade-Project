using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.ProductDTOs
{
    public class CreateProductDto
    {
        public string TitleEn { get; set; } = string.Empty;
        public string TitleAr { get; set; } = string.Empty;
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; } = 1;
        public Guid CategoryId { get; set; }
        public Guid? SubCategoryId { get; set; }
        public Guid ShopId { get; set; }
        public List<IFormFile>? Images { get; set; } // Multiple images
        public List<string>? Tags { get; set; }
        public IFormFile? ArModel { get; set; } // Optional GLB 3D model
    }
}
