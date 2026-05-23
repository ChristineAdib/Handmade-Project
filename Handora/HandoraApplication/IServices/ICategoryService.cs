using HandoraApplication.DTOs.Category_TagDTOs;
using HandoraApplication.Helpers;

namespace HandoraApplication.IServices;

public interface ICategoryService
{
    Task<Result<IEnumerable<CategoryResponseDto>>> GetAllCategories();
    Task<Result<CategoryResponseDto>> GetCategoryById(Guid id);
    Task<Result<IEnumerable<CategorySummaryDto>>> GetSubCategories(Guid parentId);
}
