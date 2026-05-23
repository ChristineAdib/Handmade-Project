using HandoraApplication.DTOs.Category_TagDTOs;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.ProductEntities;

namespace HandoraApplication.Services;

public class CategoryService(IUnitOfWork unitOfWork) : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<IEnumerable<CategoryResponseDto>>> GetAllCategories()
    {
        throw new NotImplementedException();
    }

    public async Task<Result<CategoryResponseDto>> GetCategoryById(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<IEnumerable<CategorySummaryDto>>> GetSubCategories(Guid parentId)
    {
        throw new NotImplementedException();
    }
}
