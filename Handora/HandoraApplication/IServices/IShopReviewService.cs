using HandoraApplication.DTOs.Common;
using HandoraApplication.DTOs.ShopReviewDTOs;
using HandoraApplication.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HandoraApplication.IServices
{
    public interface IShopReviewService
    {
        Task<Result<ShopReviewResponseDto>> GetReviewById(Guid id);
        Task<Result<PagedResultDto<ShopReviewResponseDto>>> GetShopReviews(Guid shopId, PaginationQueryDto query);
        Task<Result<ShopReviewResponseDto>> GetUserReviewForShop(Guid shopId, string userId);
        Task<Result<ShopReviewResponseDto>> CreateReview(CreateShopReviewDto dto, string userId);
        Task<Result<ShopReviewResponseDto>> UpdateReview(Guid id, UpdateShopReviewDto dto, string userId);
        Task<Result> DeleteReview(Guid id, string userId);
    }
}
