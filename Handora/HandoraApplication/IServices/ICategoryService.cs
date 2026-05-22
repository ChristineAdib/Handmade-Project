using HandoraApplication.Helpers;
using HandoraDomain.Models.ProductEntities;

namespace HandoraApplication.IServices;

public interface ICategoryService
{
    Task<Result<IEnumerable<Category>>> GetAllCategories();
}
