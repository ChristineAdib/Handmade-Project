using HandoraDomain.Models.ProductEntities;

namespace HandoraDomain.Interfaces;

public interface IProductRepository : IGenericRepository<Product, Guid>
{
    Task<Product?> GetProductByIDWithDetailsAsync(Guid id);
    Task<IQueryable<Product>> GetAllProductsQueryAsync();
}
