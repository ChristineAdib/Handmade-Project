using Microsoft.AspNetCore.Http;

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
        public IFormFile? ArModel { get; set; } // Optional new GLB 3D model
        public bool? RemoveArModel { get; set; } // Flag to remove the existing GLB model
    }
}
