using HandoraApplication.Services;
using HandoraDomain.Models.ProductEntities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.ProductDTOs
{
    public class UpdateProductDto
    {
        public string? TitleEn { get; set; }
        public string? TitleAr { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }

        public decimal? Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int? Quantity { get; set; }
        public ProductStatus? Status { get; set; }

        public Guid? CategoryId { get; set; }
        public Guid? SubCategoryId { get; set; }

        public List<string>? Tags { get; set; }


        public List<Guid>? RemoveImageIds { get; set; } //delete images
        public List<IFormFile>? NewImages { get; set; } //add images
    }
}
