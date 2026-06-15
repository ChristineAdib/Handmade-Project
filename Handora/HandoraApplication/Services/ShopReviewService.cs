using HandoraApplication.DTOs.Common;
using HandoraApplication.DTOs.ShopReviewDTOs;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.OrderEntity;
using HandoraDomain.Models.ShopEntities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HandoraApplication.Services
{
    public class ShopReviewService(IUnitOfWork unitOfWork) : IShopReviewService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result<ShopReviewResponseDto>> GetReviewById(Guid id)
        {
            var repo = _unitOfWork.Repository<ShopReview, Guid>();
            var query = await repo.GetAllAsNoTracking();

            var review = await query
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review is null)
                return Result<ShopReviewResponseDto>.Failure("Review not found");

            var dto = new ShopReviewResponseDto
            {
                Id = review.Id,
                Rating = review.Rating,
                Comment = review.Comment,
                UserName = review.User.UserName ?? string.Empty,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt
            };

            return Result<ShopReviewResponseDto>.Success(dto);
        }

        public async Task<Result<PagedResultDto<ShopReviewResponseDto>>> GetShopReviews(Guid shopId, PaginationQueryDto query)
        {
            var repo = _unitOfWork.Repository<ShopReview, Guid>();
            var allReviews = await repo.GetAllAsNoTracking();

            var shopReviews = allReviews
                .Include(r => r.User)
                .Where(r => r.ShopId == shopId && r.IsApproved && !r.IsDeleted);

            var totalCount = await shopReviews.CountAsync();

            var reviews = await shopReviews
                .OrderByDescending(r => r.CreatedAt)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var dtos = reviews.Select(r => new ShopReviewResponseDto
            {
                Id = r.Id,
                Rating = r.Rating,
                Comment = r.Comment,
                UserName = r.User.UserName ?? string.Empty,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }).ToList();

            var result = new PagedResultDto<ShopReviewResponseDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };

            return Result<PagedResultDto<ShopReviewResponseDto>>.Success(result);
        }

        public async Task<Result<ShopReviewResponseDto>> GetUserReviewForShop(Guid shopId, string userId)
        {
            var repo = _unitOfWork.Repository<ShopReview, Guid>();
            var query = await repo.GetAllAsNoTracking();

            var review = await query
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ShopId == shopId && r.UserId == userId && !r.IsDeleted);

            if (review is null)
                return Result<ShopReviewResponseDto>.Failure("User has not reviewed this shop yet");

            var dto = new ShopReviewResponseDto
            {
                Id = review.Id,
                Rating = review.Rating,
                Comment = review.Comment,
                UserName = review.User.UserName ?? string.Empty,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt
            };

            return Result<ShopReviewResponseDto>.Success(dto);
        }

        public async Task<Result<ShopReviewResponseDto>> CreateReview(CreateShopReviewDto dto, string userId)
        {
            // 1. Validation rating
            if (dto.Rating < 1 || dto.Rating > 5)
                return Result<ShopReviewResponseDto>.Failure("Rating must be between 1 and 5");

            // 2. Validate shop exists
            var shopRepo = _unitOfWork.Repository<Shop, Guid>();
            var shop = await shopRepo.GetByIdAsync(dto.ShopId);
            if (shop is null)
                return Result<ShopReviewResponseDto>.Failure("Shop not found");

            // Check if reviewer is the owner of the shop
            if (shop.OwnerId == userId)
                return Result<ShopReviewResponseDto>.Failure("You cannot review your own shop.");

            // 3. Purchase validation: only users who completed an order from that shop can review
            var orderRepo = _unitOfWork.Repository<Order, Guid>();
            var ordersQuery = await orderRepo.GetAllAsNoTracking();
            var hasCompletedOrder = await ordersQuery
                .AnyAsync(o => o.UserId == userId && 
                               o.Status == OrderStatus.Delivered && 
                               o.Items.Any(i => i.ShopId == dto.ShopId));

            if (!hasCompletedOrder)
                return Result<ShopReviewResponseDto>.Failure("Only users who completed an order from this shop can leave a review");

            // 4. Validate user hasn't already reviewed this shop
            var reviewRepo = _unitOfWork.Repository<ShopReview, Guid>();
            var existingQuery = await reviewRepo.GetAllAsNoTracking();
            var alreadyReviewed = await existingQuery
                .AnyAsync(r => r.ShopId == dto.ShopId && r.UserId == userId && !r.IsDeleted);

            if (alreadyReviewed)
                return Result<ShopReviewResponseDto>.Failure("You have already reviewed this shop");

            // 5. Create shop review
            var review = new ShopReview
            {
                Id = Guid.NewGuid(),
                ShopId = dto.ShopId,
                UserId = userId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                IsApproved = true
            };

            await reviewRepo.AddAsync(review);
            await _unitOfWork.SaveChangesAsync();

            // 6. Update Shop statistics
            await UpdateShopRatingAndSave(dto.ShopId);

            // 7. Return saved review
            var savedQuery = await reviewRepo.GetAllAsNoTracking();
            var savedReview = await savedQuery
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == review.Id);

            var responseDto = new ShopReviewResponseDto
            {
                Id = savedReview!.Id,
                Rating = savedReview.Rating,
                Comment = savedReview.Comment,
                UserName = savedReview.User.UserName ?? string.Empty,
                CreatedAt = savedReview.CreatedAt,
                UpdatedAt = savedReview.UpdatedAt
            };

            return Result<ShopReviewResponseDto>.Success(responseDto);
        }

        public async Task<Result<ShopReviewResponseDto>> UpdateReview(Guid id, UpdateShopReviewDto dto, string userId)
        {
            // 1. Validation rating
            if (dto.Rating < 1 || dto.Rating > 5)
                return Result<ShopReviewResponseDto>.Failure("Rating must be between 1 and 5");

            var repo = _unitOfWork.Repository<ShopReview, Guid>();
            var review = await repo.GetByIdAsync(id);

            if (review is null)
                return Result<ShopReviewResponseDto>.Failure("Review not found");

            if (review.UserId != userId)
                return Result<ShopReviewResponseDto>.Failure("You are not allowed to update this review");

            // Check if user is the owner of the shop being reviewed
            var shopRepo = _unitOfWork.Repository<Shop, Guid>();
            var shop = await shopRepo.GetByIdAsync(review.ShopId);
            if (shop != null && shop.OwnerId == userId)
                return Result<ShopReviewResponseDto>.Failure("You cannot review your own shop.");

            // Update fields
            review.Rating = dto.Rating;
            review.Comment = dto.Comment;
            review.UpdatedAt = DateTime.UtcNow;
            review.UpdatedBy = userId;

            await repo.UpdateAsync(review);
            await _unitOfWork.SaveChangesAsync();

            // Update Shop statistics
            await UpdateShopRatingAndSave(review.ShopId);

            // Return updated DTO
            var savedQuery = await repo.GetAllAsNoTracking();
            var savedReview = await savedQuery
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == review.Id);

            var responseDto = new ShopReviewResponseDto
            {
                Id = savedReview!.Id,
                Rating = savedReview.Rating,
                Comment = savedReview.Comment,
                UserName = savedReview.User.UserName ?? string.Empty,
                CreatedAt = savedReview.CreatedAt,
                UpdatedAt = savedReview.UpdatedAt
            };

            return Result<ShopReviewResponseDto>.Success(responseDto);
        }

        public async Task<Result> DeleteReview(Guid id, string userId)
        {
            var repo = _unitOfWork.Repository<ShopReview, Guid>();
            var review = await repo.GetByIdAsync(id);

            if (review is null)
                return Result.Failure("Review not found");

            if (review.UserId != userId)
                return Result.Failure("You are not allowed to delete this review");

            var shopId = review.ShopId;

            // Soft-delete review
            await repo.SoftDeleteAsync(review);
            await _unitOfWork.SaveChangesAsync();

            // Update Shop statistics
            await UpdateShopRatingAndSave(shopId);

            return Result.Success();
        }

        private async Task UpdateShopRatingAndSave(Guid shopId)
        {
            var shopRepo = _unitOfWork.Repository<Shop, Guid>();
            var shop = await shopRepo.GetByIdAsync(shopId);

            if (shop is not null)
            {
                var reviewRepo = _unitOfWork.Repository<ShopReview, Guid>();
                var allReviews = await reviewRepo.GetAllAsNoTracking();
                var ratingsQuery = allReviews.Where(r => r.ShopId == shopId && !r.IsDeleted);
                
                var count = await ratingsQuery.CountAsync();
                shop.ReviewCount = count;
                shop.Rating = count > 0
                    ? (decimal)await ratingsQuery.AverageAsync(r => (double)r.Rating)
                    : 0;

                await shopRepo.UpdateAsync(shop);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
