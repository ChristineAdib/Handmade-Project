using HandoraApplication.Helpers;
using HandoraDomain.Models.ProductEntities;

namespace HandoraApplication.IServices;

public interface IProductService
{
    Task<Result<Product>> GetProduct(Guid id);
    Task<Result<IEnumerable<Product>>> GetProducts();
}
