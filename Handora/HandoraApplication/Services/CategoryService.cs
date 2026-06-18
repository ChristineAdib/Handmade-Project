using HandoraApplication.DTOs.Category_TagDTOs;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.ProductEntities;
using Microsoft.EntityFrameworkCore;

namespace HandoraApplication.Services;

public class CategoryService(IUnitOfWork unitOfWork) : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<IEnumerable<CategoryResponseDto>>> GetAllCategories()
    {
        var repo = _unitOfWork.Repository<Category, Guid>();
        var query = await repo.GetAllAsNoTracking();

        var categories = await query
    .Include(c => c.SubCategories)
    .Where(c => c.ParentId == null && !c.IsDeleted)
    .ToListAsync();

        var result = categories.Select(c => new CategoryResponseDto
        {
            Id = c.Id,
            NameEn = c.NameEn,
            NameAr = c.NameAr,
            ImageUrl = c.ImageUrl,
            ParentId = c.ParentId,
            SubCategories = c.SubCategories
    .Where(sub => !sub.IsDeleted)
    .Select(sub => new CategorySummaryDto
    {
        Id = sub.Id,
        NameEn = sub.NameEn,
        NameAr = sub.NameAr
    }).ToList()
        });

        return Result<IEnumerable<CategoryResponseDto>>.Success(result);
    }

    public async Task<Result<CategoryResponseDto>> GetCategoryById(Guid id)
    {
        var repo = _unitOfWork.Repository<Category, Guid>();
        var query = await repo.GetAllAsNoTracking();

        var category = await query
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
            return Result<CategoryResponseDto>.Failure("Category not found");

        var dto = new CategoryResponseDto
        {
            Id = category.Id,
            NameEn = category.NameEn,
            NameAr = category.NameAr,
            ImageUrl = category.ImageUrl,
            ParentId = category.ParentId,
            SubCategories = category.SubCategories.Select(sub => new CategorySummaryDto
            {
                Id = sub.Id,
                NameEn = sub.NameEn,
                NameAr = sub.NameAr
            }).ToList()
        };

        return Result<CategoryResponseDto>.Success(dto);
    }

    public async Task<Result<IEnumerable<CategorySummaryDto>>> GetSubCategories(Guid parentId)
    {
        var repo = _unitOfWork.Repository<Category, Guid>();
        var query = await repo.GetAllAsNoTracking();

        var subCategories = await query
            .Where(c => c.ParentId == parentId)
            .ToListAsync();

        var result = subCategories.Select(c => new CategorySummaryDto
        {
            Id = c.Id,
            NameEn = c.NameEn,
            NameAr = c.NameAr
        });

        return Result<IEnumerable<CategorySummaryDto>>.Success(result);
    }

    public async Task<Result<CategoryResponseDto>> CreateCategory(CreateCategoryDto dto)
    {
        var repo = _unitOfWork.Repository<Category, Guid>();

        var category = new Category
        {
            Id = Guid.NewGuid(),
            NameEn = dto.NameEn,
            NameAr = dto.NameAr,
            ImageUrl = dto.ImageUrl,
            ParentId = dto.ParentId
        };

        await repo.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return Result<CategoryResponseDto>.Success(new CategoryResponseDto
        {
            Id = category.Id,
            NameEn = category.NameEn,
            NameAr = category.NameAr,
            ImageUrl = category.ImageUrl,
            ParentId = category.ParentId
        });
    }

    public async Task<Result<CategoryResponseDto>> UpdateCategory(Guid id, UpdateCategoryDto dto)
    {
        var repo = _unitOfWork.Repository<Category, Guid>();
        var category = await repo.GetByIdAsync(id);

        if (category is null)
            return Result<CategoryResponseDto>.Failure("Category not found");

        category.NameEn = dto.NameEn;
        category.NameAr = dto.NameAr;
        category.ImageUrl = dto.ImageUrl;
        category.ParentId = dto.ParentId;

        await repo.UpdateAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return Result<CategoryResponseDto>.Success(new CategoryResponseDto
        {
            Id = category.Id,
            NameEn = category.NameEn,
            NameAr = category.NameAr,
            ImageUrl = category.ImageUrl,
            ParentId = category.ParentId
        });
    }

    public async Task<Result<bool>> DeleteCategory(Guid id)
    {
        var repo = _unitOfWork.Repository<Category, Guid>();
        var category = await repo.GetByIdAsync(id);

        if (category is null)
            return Result<bool>.Failure("Category not found");

        await repo.SoftDeleteAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}