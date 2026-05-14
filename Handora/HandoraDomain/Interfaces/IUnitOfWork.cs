using HandoraDomain.Models;

namespace HandoraDomain.Interfaces;

public interface IUnitOfWork
{
    IGenericRepository<TEntity, TId> Repository<TEntity, TId>() where TEntity : BaseEntity<TId>;
    Task CreateTransactionAsync();
    Task CommitAsync();
    Task CreateSavePointAsync(string point);
    Task RollbackAsync();
    Task RollbackToSavePointAsync(string point);
    Task<bool> SaveChangesAsync();
}
