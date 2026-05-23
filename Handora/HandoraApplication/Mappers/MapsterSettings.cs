using HandoraApplication.DTOs.ProductDTOs;
using HandoraApplication.DTOs.ReviewDTOs;
using HandoraDomain.Models.ProductEntities;
using Mapster;

namespace HandoraApplication.Mappers;

public class MapsterSettings
{
    public static void Configure()
    {
        TypeAdapterConfig<Product, ProductResponseDto>.NewConfig()
            .Map(dest => dest.CategoryNameEn, src => src.Category.NameEn)
            .Map(dest => dest.CategoryNameAr, src => src.Category.NameAr)
            .Map(dest => dest.ShopName, src => src.Shop.Name)
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.Tags, src => src.Tags.Select(t => t.Name).ToList())
            .Map(dest => dest.Images, src => src.Images.Adapt<List<ProductImageDto>>())
            .Map(dest => dest.LatestReviews, src => src.Reviews
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .Adapt<List<ReviewSummaryDto>>());

        TypeAdapterConfig<Review, ReviewSummaryDto>.NewConfig()
            .Map(dest => dest.UserName, src => src.User.UserName ?? "Anonymous");

        TypeAdapterConfig<Product, ProductSummaryDto>.NewConfig()
            .Map(dest => dest.CategoryNameEn, src => src.Category.NameEn)
            .Map(dest => dest.ShopName, src => src.Shop.Name)
            .Map(dest => dest.MainImageUrl, src => src.Images
                .Where(i => i.IsMain)
                .Select(i => i.ImageUrl)
                .FirstOrDefault() ?? src.Images.Select(i => i.ImageUrl).FirstOrDefault());
    }
}
