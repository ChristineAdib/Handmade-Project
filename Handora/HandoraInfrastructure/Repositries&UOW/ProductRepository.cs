using HandoraDomain.Interfaces;
using HandoraDomain.Models.ProductEntities;
using HandoraInfrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HandoraInfrastructure.Repositries;

public class ProductRepository(AppDbContext context)
    : GenericRepository<Product, Guid>(context), IProductRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Product?> GetProductByIDWithDetailsAsync(Guid id)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Shop)
            .Include(p => p.Images)
            .Include(p => p.Tags)
            .Include(p => p.Reviews.OrderByDescending(r => r.CreatedAt).Take(5))
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    public async Task<IQueryable<Product>> GetAllProductsQueryAsync()
    {
        return _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Shop)
            .Include(p => p.Images)
            .Include(p => p.Tags)
            .Where(p => !p.IsDeleted);
    }
}
