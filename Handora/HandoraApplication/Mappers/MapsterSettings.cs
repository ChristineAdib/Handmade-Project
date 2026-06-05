using HandoraApplication.DTOs.CouponDTOs;
using HandoraApplication.DTOs.ProductDTOs;
using HandoraApplication.DTOs.ReviewDTOs;
using HandoraApplication.DTOs.ShopDTOs;
using HandoraApplication.DTOs.WishlistDTOs;
using HandoraDomain.Models.CouponEntities;
using HandoraDomain.Models.ProductEntities;
using HandoraDomain.Models.ShopEntities;
using HandoraDomain.Models.WishListEntoties;
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

        TypeAdapterConfig<Shop, ShopDto>.NewConfig()
    .Map(dest => dest.OwnerName, src => src.Owner.Name)
    .Map(dest => dest.ProductCount, src => src.Products.Count(p => !p.IsDeleted));

        TypeAdapterConfig<Shop, ShopWithProductsDto>.NewConfig()
            .Map(dest => dest.OwnerName, src => src.Owner.Name)
            .Map(dest => dest.Products, src => src.Products
                .Where(p => !p.IsDeleted)
                .Adapt<List<ProductSummaryDto>>());

        TypeAdapterConfig<WishList, WishListDto>.NewConfig()
    .Map(dest => dest.Items, src => src.Items
        .Where(i => !i.IsDeleted)
        .Adapt<List<WishListItemDto>>());

        TypeAdapterConfig<WishListItem, WishListItemDto>.NewConfig()
            .Map(dest => dest.TitleEn, src => src.Product.TitleEn)
            .Map(dest => dest.TitleAr, src => src.Product.TitleAr)
            .Map(dest => dest.Price, src => src.Product.Price)
            .Map(dest => dest.DiscountPrice, src => src.Product.DiscountPrice)
            .Map(dest => dest.ImageUrl, src => src.Product.Images
                .Where(i => i.IsMain)
                .Select(i => i.ImageUrl)
                .FirstOrDefault());

        // Mapping for Coupon to CouponResponseDto
        TypeAdapterConfig<Coupon, CouponResponseDto>.NewConfig()
            .Map(dest => dest.DiscountType, src => src.DiscountType.ToString());
    }
}
















