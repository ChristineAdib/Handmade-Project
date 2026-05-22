using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.ProductEntities;

namespace HandoraApplication.Services;

public class ProductService(IUnitOfWork unitOfWork) : IProductService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    public async Task<Result<Product>> GetProduct(Guid id)
    {
        var product = await _unitOfWork.Repository<Product, Guid>().GetByIdAsync(id);
        return product is null ? Result<Product>.Failure("Product not found") : Result<Product>.Success(product);
    }

    public async Task<Result<IEnumerable<Product>>> GetProducts()
    {
        var products = await _unitOfWork.Repository<Product, Guid>().GetAllAsNoTracking();
        return Result<IEnumerable<Product>>.Success([..products]);
    }
}
