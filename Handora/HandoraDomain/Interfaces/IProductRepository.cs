using HandoraDomain.Models.ProductEntities;

namespace HandoraDomain.Interfaces;

public interface IProductRepository : IGenericRepository<Product, Guid>
{
    Task<Product?> GetProductByIDWithDetailsAsync(Guid id);
    Task<IQueryable<Product>> GetAllProductsQueryAsync();
    Task<IEnumerable<Product>> GetProductsByIdsAsync(IEnumerable<Guid> ids);
    void RemoveProductImage(ProductImage image);
    void AddProductImage(ProductImage image);
    void SetImageUnchanged(ProductImage image);
    void ForceDetectChanges();
    void DisableAutoDetectChanges();
    void EnableAutoDetectChanges();

    // Draft operations
    Task<ProductDraft?> GetPendingDraftByProductIdAsync(Guid productId);
    Task AddDraftAsync(ProductDraft draft);
    void RemoveDraft(ProductDraft draft);
}
