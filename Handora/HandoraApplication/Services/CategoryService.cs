using System.Threading.Tasks;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.ProductEntities;

namespace HandoraApplication.Services;

public class CategoryService(IUnitOfWork unitOfWork) : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    public async Task<Result<IEnumerable<Category>>> GetAllCategories()
    {
        var categories =await _unitOfWork.Repository<Category, Guid>().GetAllAsNoTracking();
        return Result<IEnumerable<Category>>.Success(categories);
    }
}
