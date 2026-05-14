using HandoraDomain.Interfaces;
using HandoraDomain.Models;
using HandoraInfrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HandoraInfrastructure.Repositries;

public class GenericRepository<TEntity, TId>(AppDbContext context)
    : IGenericRepository<TEntity, TId> where TEntity
    : BaseEntity<TId>
{
    private readonly AppDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();

    public async Task AddAsync(TEntity entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public async Task UpdateAsync(TEntity entity)
    {
        _dbSet.Update(entity);
    }

    public async Task SoftDeleteAsync(TEntity entity)
    {
        entity.IsDeleted = true;
        _dbSet.Update(entity);
    }

    public async Task HardDeleteAsync(TEntity entity)
    {
        _context.Remove(entity);
    }

    public async Task<IQueryable<TEntity>> GetAllAsNoTracking()
    {
        return _dbSet.AsNoTracking();
    }

    public async Task<IQueryable<TEntity>> GetAllAsync()
    {
        return _dbSet;
    }

    public async Task<TEntity?> GetByIdAsync(TId id)
    {
        return await _dbSet.FindAsync(id);
    }

}
