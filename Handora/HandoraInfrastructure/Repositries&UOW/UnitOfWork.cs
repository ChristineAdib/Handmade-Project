using System.Collections.Concurrent;
using HandoraDomain.Interfaces;
using HandoraDomain.Models;
using HandoraInfrastructure.Data;
using HandoraInfrastructure.Repositries;
using Microsoft.EntityFrameworkCore.Storage;

namespace HandoraInfrastructure.Repositries_UOW;

public class UnitOfWork : IUnitOfWork
{
    private readonly ConcurrentDictionary<string, object> _repositories = new();
    private IDbContextTransaction? transaction;

    private readonly AppDbContext _context;


    public IGenericRepository<TEntity, TId> Repository<TEntity, TId>() where TEntity : BaseEntity<TId>
    {
        var type = typeof(TEntity).Name;
        return (IGenericRepository<TEntity, TId>)_repositories.GetOrAdd(type, t =>
        {
            var repositoryType = typeof(GenericRepository<,>).MakeGenericType(typeof(TEntity), typeof(TId));
            return Activator.CreateInstance(repositoryType, _context)
                ?? throw new InvalidOperationException($"Could not create instance of {repositoryType}");
        });
    }

    public async Task CreateTransactionAsync()
    {
        transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        await transaction!.CommitAsync();
    }

    public async Task CreateSavePointAsync(string point)
    {
        await transaction!.CreateSavepointAsync(point);
    }

    public async Task RollbackAsync()
    {
        await transaction!.RollbackAsync();
    }

    public async Task RollbackToSavePointAsync(string point)
    {
        await transaction!.RollbackToSavepointAsync(point);
    }


    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
