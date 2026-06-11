using HandoraApplication.DTOs.Category_TagDTOs;
using HandoraApplication.Helpers;

namespace HandoraApplication.IServices;

public interface ICategoryService
{
    Task<Result<IEnumerable<CategoryResponseDto>>> GetAllCategories();
    Task<Result<CategoryResponseDto>> GetCategoryById(Guid id);
    Task<Result<IEnumerable<CategorySummaryDto>>> GetSubCategories(Guid parentId);
    Task<Result<CategoryResponseDto>> CreateCategory(CreateCategoryDto dto);
    Task<Result<CategoryResponseDto>> UpdateCategory(Guid id, UpdateCategoryDto dto);
    Task<Result<bool>> DeleteCategory(Guid id);
}
