using HandoraApplication.DTOs.Common;
using HandoraApplication.DTOs.ReviewDTOs;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.ProductEntities;
using HandoraDomain.Models.OrderEntity;
using Microsoft.EntityFrameworkCore;

namespace HandoraApplication.Services;

public class ReviewService(IUnitOfWork unitOfWork) : IReviewService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<ReviewResponseDto>> GetReviewById(Guid id)
    {
        var repo = _unitOfWork.Repository<Review, Guid>();
        var query = await repo.GetAllAsNoTracking();

        var review = await query
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review is null)
            return Result<ReviewResponseDto>.Failure("Review not found");

        var dto = new ReviewResponseDto
        {
            Id = review.Id,
            Rating = review.Rating,
            Comment = review.Comment,
            UserName = review.User.UserName ?? string.Empty,
            CreatedAt = review.CreatedAt,
            IsVerifiedPurchase = review.IsVerifiedPurchase
        };

        return Result<ReviewResponseDto>.Success(dto);
    }

    public async Task<Result<PagedResultDto<ReviewResponseDto>>> GetProductReviews(Guid productId, PaginationQueryDto query)
    {
        var repo = _unitOfWork.Repository<Review, Guid>();
        var allReviews = await repo.GetAllAsNoTracking();

        var productReviews = allReviews
            .Include(r => r.User)
            .Where(r => r.ProductId == productId && r.IsApproved);

        var totalCount = await productReviews.CountAsync();

        var reviews = await productReviews
            .OrderByDescending(r => r.CreatedAt)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = reviews.Select(r => new ReviewResponseDto
        {
            Id = r.Id,
            Rating = r.Rating,
            Comment = r.Comment,
            UserName = r.User.UserName ?? string.Empty,
            CreatedAt = r.CreatedAt,
            IsVerifiedPurchase = r.IsVerifiedPurchase
        }).ToList();

        var result = new PagedResultDto<ReviewResponseDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };

        return Result<PagedResultDto<ReviewResponseDto>>.Success(result);
    }

    public async Task<Result<ReviewResponseDto>> CreateReview(CreateReviewDto dto, string userId)
    {
        // 1. Rating must be between 1 and 5
        if (dto.Rating < 1 || dto.Rating > 5)
            return Result<ReviewResponseDto>.Failure("Rating must be between 1 and 5");

        // 2. Product exists
        var productRepo = _unitOfWork.Repository<Product, Guid>();
        var productQuery = await productRepo.GetAllAsync();
        var product = await productQuery
            .Include(p => p.Shop)
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId && !p.IsDeleted);

        if (product is null)
            return Result<ReviewResponseDto>.Failure("Product not found");

        // Check if reviewer owns the product
        if (product.Shop.OwnerId == userId)
            return Result<ReviewResponseDto>.Failure("You cannot review your own product.");

        // 3. Check purchase eligibility (must have a delivered, non-cancelled/refunded order containing this product)
        var eligibilityResult = await GetReviewEligibility(dto.ProductId, userId);
        if (!eligibilityResult.IsSuccess || eligibilityResult.Data?.IsEligible != true)
            return Result<ReviewResponseDto>.Failure("You can only review products that you have purchased and received.");

        // 4. Duplicate review prevention
        if (eligibilityResult.Data?.AlreadyReviewed == true)
            return Result<ReviewResponseDto>.Failure("You have already reviewed this product");

        // 5. Create review
        var review = new Review
        {
            Id = Guid.NewGuid(),
            ProductId = dto.ProductId,
            UserId = userId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            IsApproved = true,
            IsVerifiedPurchase = true // Always true because of the eligibility check above
        };

        var reviewRepo = _unitOfWork.Repository<Review, Guid>();
        await reviewRepo.AddAsync(review);
        await _unitOfWork.SaveChangesAsync();

        // 6. Recalculate AverageRating and Count for Product
        var allReviews = await reviewRepo.GetAllAsNoTracking();
        var ratingsQuery = allReviews.Where(r => r.ProductId == dto.ProductId);
        var count = await ratingsQuery.CountAsync();

        product.ReviewCount = count;
        product.AverageRating = count > 0 
            ? (decimal)await ratingsQuery.AverageAsync(r => (double)r.Rating)
            : (decimal)dto.Rating;
            
        await productRepo.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();

        // 7. Get review with user details
        var savedQuery = await reviewRepo.GetAllAsNoTracking();
        var savedReview = await savedQuery
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == review.Id);

        var responseDto = new ReviewResponseDto
        {
            Id = savedReview!.Id,
            Rating = savedReview.Rating,
            Comment = savedReview.Comment,
            UserName = savedReview.User.UserName ?? string.Empty,
            CreatedAt = savedReview.CreatedAt,
            IsVerifiedPurchase = savedReview.IsVerifiedPurchase
        };

        return Result<ReviewResponseDto>.Success(responseDto);
    }

    public async Task<Result<ReviewResponseDto>> UpdateReview(Guid id, CreateReviewDto dto, string userId)
    {
        // 1. Rating must be between 1 and 5
        if (dto.Rating < 1 || dto.Rating > 5)
            return Result<ReviewResponseDto>.Failure("Rating must be between 1 and 5");

        var repo = _unitOfWork.Repository<Review, Guid>();
        var review = await repo.GetByIdAsync(id);

        // 2. Review exists
        if (review is null)
            return Result<ReviewResponseDto>.Failure("Review not found");

        // 3. User ownership validation
        if (review.UserId != userId)
            return Result<ReviewResponseDto>.Failure("You are not allowed to update this review");

        // Check if user owns the product of the review
        var checkProductRepo = _unitOfWork.Repository<Product, Guid>();
        var checkProductQuery = await checkProductRepo.GetAllAsNoTracking();
        var checkProduct = await checkProductQuery
            .Include(p => p.Shop)
            .FirstOrDefaultAsync(p => p.Id == review.ProductId && !p.IsDeleted);

        if (checkProduct != null && checkProduct.Shop.OwnerId == userId)
            return Result<ReviewResponseDto>.Failure("You cannot review your own product.");

        // 4. Update fields
        review.Rating = dto.Rating;
        review.Comment = dto.Comment;
        review.UpdatedAt = DateTime.UtcNow;
        review.UpdatedBy = userId;

        await repo.UpdateAsync(review);
        await _unitOfWork.SaveChangesAsync();

        // 5. Recalculate AverageRating and Count for Product
        var productRepo = _unitOfWork.Repository<Product, Guid>();
        var product = await productRepo.GetByIdAsync(review.ProductId);
        if (product is not null)
        {
            var allReviews = await repo.GetAllAsNoTracking();
            var ratingsQuery = allReviews.Where(r => r.ProductId == review.ProductId);
            var count = await ratingsQuery.CountAsync();

            product.ReviewCount = count;
            product.AverageRating = count > 0 
                ? (decimal)await ratingsQuery.AverageAsync(r => (double)r.Rating)
                : 0;

            await productRepo.UpdateAsync(product);
            await _unitOfWork.SaveChangesAsync();
        }

        // 6. Get updated review details
        var savedQuery = await repo.GetAllAsNoTracking();
        var savedReview = await savedQuery
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == review.Id);

        var responseDto = new ReviewResponseDto
        {
            Id = savedReview!.Id,
            Rating = savedReview.Rating,
            Comment = savedReview.Comment,
            UserName = savedReview.User.UserName ?? string.Empty,
            CreatedAt = savedReview.CreatedAt,
            IsVerifiedPurchase = savedReview.IsVerifiedPurchase
        };

        return Result<ReviewResponseDto>.Success(responseDto);
    }

    public async Task<Result> DeleteReview(Guid id, string userId)
    {
        var repo = _unitOfWork.Repository<Review, Guid>();
        var review = await repo.GetByIdAsync(id);

        // 1. Review exists
        if (review is null)
            return Result.Failure("Review not found");

        // 2. User ownership validation
        if (review.UserId != userId)
            return Result.Failure("You are not allowed to delete this review");

        // 3. Soft delete review
        await repo.SoftDeleteAsync(review);

        // 4. Recalculate average rating
        var productRepo = _unitOfWork.Repository<Product, Guid>();
        var product = await productRepo.GetByIdAsync(review.ProductId);

        if (product is not null)
        {
            var allReviews = await repo.GetAllAsNoTracking();
            var ratingsQuery = allReviews.Where(r => r.ProductId == review.ProductId);
            var count = await ratingsQuery.CountAsync();

            product.ReviewCount = count;
            product.AverageRating = count > 0
                ? (decimal)await ratingsQuery.AverageAsync(r => (double)r.Rating)
                : 0;

            await productRepo.UpdateAsync(product);
        }

        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<IEnumerable<UserReviewDto>>> GetUserReviews(string userId)
    {
        var repo = _unitOfWork.Repository<Review, Guid>();
        var query = await repo.GetAllAsNoTracking();

        var userReviews = await query
            .Include(r => r.User)
            .Include(r => r.Product)
            .ThenInclude(p => p.Images)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var dtos = userReviews.Select(r => new UserReviewDto
        {
            Id = r.Id,
            Rating = r.Rating,
            Comment = r.Comment,
            ProductId = r.ProductId,
            ProductTitle = r.Product.TitleEn,
            ProductImage = r.Product.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl ?? r.Product.Images.FirstOrDefault()?.ImageUrl,
            CreatedAt = r.CreatedAt,
            IsVerifiedPurchase = r.IsVerifiedPurchase
        });

        return Result<IEnumerable<UserReviewDto>>.Success(dtos);
    }

    public async Task<Result<ReviewEligibilityDto>> GetReviewEligibility(Guid productId, string userId)
    {
        // 1. Check order eligibility (delivered purchase)
        var orderRepo = _unitOfWork.Repository<Order, Guid>();
        var ordersQuery = await orderRepo.GetAllAsNoTracking();
        var hasDeliveredOrder = await ordersQuery
            .AnyAsync(o => o.UserId == userId 
                        && o.Status == OrderStatus.Delivered 
                        && o.Items.Any(item => item.Product.ProductId == productId));

        if (!hasDeliveredOrder)
        {
            return Result<ReviewEligibilityDto>.Success(new ReviewEligibilityDto
            {
                IsEligible = false,
                AlreadyReviewed = false
            });
        }

        // 2. Check if already reviewed
        var reviewRepo = _unitOfWork.Repository<Review, Guid>();
        var reviewsQuery = await reviewRepo.GetAllAsNoTracking();
        var existingReview = await reviewsQuery
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);

        if (existingReview is not null)
        {
            return Result<ReviewEligibilityDto>.Success(new ReviewEligibilityDto
            {
                IsEligible = true,
                AlreadyReviewed = true,
                ExistingReviewId = existingReview.Id,
                ExistingRating = existingReview.Rating,
                ExistingComment = existingReview.Comment
            });
        }

        return Result<ReviewEligibilityDto>.Success(new ReviewEligibilityDto
        {
            IsEligible = true,
            AlreadyReviewed = false
        });
    }
}