using HandoraApplication.DTOs.Common;
using HandoraApplication.DTOs.ReviewDTOs;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;

namespace HandoraApplication.Services;

public class ReviewService(IUnitOfWork unitOfWork) : IReviewService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<ReviewResponseDto>> GetReviewById(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<PagedResultDto<ReviewResponseDto>>> GetProductReviews(Guid productId, PaginationQueryDto query)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<ReviewResponseDto>> CreateReview(CreateReviewDto dto, string userId)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> DeleteReview(Guid id, string userId)
    {
        throw new NotImplementedException();
    }
}
