using HandoraApplication.DTOs.Common;
using HandoraApplication.DTOs.ReviewDTOs;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.ProductEntities;
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
            CreatedAt = review.CreatedAt
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
            CreatedAt = r.CreatedAt
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
        // 1. تأكد إن ال Rating بين 1 و 5
        if (dto.Rating < 1 || dto.Rating > 5)
            return Result<ReviewResponseDto>.Failure("Rating must be between 1 and 5");

        // 2. تأكد إن المنتج موجود
        var productRepo = _unitOfWork.Repository<Product, Guid>();
        var product = await productRepo.GetByIdAsync(dto.ProductId);
        if (product is null)
            return Result<ReviewResponseDto>.Failure("Product not found");

        // 3. تأكد إن المستخدم معملش review قبل كده
        var reviewRepo = _unitOfWork.Repository<Review, Guid>();
        var existingQuery = await reviewRepo.GetAllAsNoTracking();
        var alreadyReviewed = await existingQuery
            .AnyAsync(r => r.ProductId == dto.ProductId && r.UserId == userId);

        if (alreadyReviewed)
            return Result<ReviewResponseDto>.Failure("You have already reviewed this product");

        // 4. اعمل ال Review
        var review = new Review
        {
            Id = Guid.NewGuid(),
            ProductId = dto.ProductId,
            UserId = userId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            IsApproved = true
        };

        await reviewRepo.AddAsync(review);

        // 5. اعمل Update ل AverageRating بتاع المنتج
        var allReviews = await reviewRepo.GetAllAsNoTracking();
        var ratingsQuery = allReviews.Where(r => r.ProductId == dto.ProductId);
        var avgRating = await ratingsQuery.AverageAsync(r => (double)r.Rating);

        product.AverageRating = (decimal)avgRating;
        product.ReviewCount = await ratingsQuery.CountAsync();
        await productRepo.UpdateAsync(product);

        await _unitOfWork.SaveChangesAsync();

        // 6. جيب الـ review مع بيانات الـ User
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
            CreatedAt = savedReview.CreatedAt
        };

        return Result<ReviewResponseDto>.Success(responseDto);
    }

    public async Task<Result> DeleteReview(Guid id, string userId)
    {
        var repo = _unitOfWork.Repository<Review, Guid>();
        var review = await repo.GetByIdAsync(id);

        // 1. تأكد إن ال Review موجود
        if (review is null)
            return Result.Failure("Review not found");

        // 2. تأكد إن المستخدم ده هو اللي عمله
        if (review.UserId != userId)
            return Result.Failure("You are not allowed to delete this review");

        // 3. احذف ال Review
        await repo.SoftDeleteAsync(review);

        // 4. اعمل Update ل AverageRating
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
            CreatedAt = r.CreatedAt
        });

        return Result<IEnumerable<UserReviewDto>>.Success(dtos);
    }
}