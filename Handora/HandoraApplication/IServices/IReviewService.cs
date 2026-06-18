using HandoraApplication.DTOs.Common;
using HandoraApplication.DTOs.ReviewDTOs;
using HandoraApplication.Helpers;

namespace HandoraApplication.IServices;

public interface IReviewService
{
    Task<Result<ReviewResponseDto>> GetReviewById(Guid id);
    Task<Result<PagedResultDto<ReviewResponseDto>>> GetProductReviews(Guid productId, PaginationQueryDto query);
    Task<Result<ReviewResponseDto>> CreateReview(CreateReviewDto dto, string userId);
    Task<Result<ReviewResponseDto>> UpdateReview(Guid id, CreateReviewDto dto, string userId);
    Task<Result> DeleteReview(Guid id, string userId);
    Task<Result<IEnumerable<UserReviewDto>>> GetUserReviews(string userId);
    Task<Result<ReviewEligibilityDto>> GetReviewEligibility(Guid productId, string userId);
}
