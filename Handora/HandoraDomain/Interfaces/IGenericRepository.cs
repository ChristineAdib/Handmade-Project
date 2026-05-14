using HandoraDomain.Models;

namespace HandoraDomain.Interfaces;

public interface IGenericRepository<TEntity, TId> where TEntity : BaseEntity<TId>
    {
        public Task AddAsync(TEntity entity);

        public Task UpdateAsync(TEntity entity);

        public Task SoftDeleteAsync(TEntity entity);

        public Task HardDeleteAsync(TEntity entity);

        public Task<IQueryable<TEntity>> GetAllAsNoTracking();
        public Task<IQueryable<TEntity>> GetAllAsync();
    }