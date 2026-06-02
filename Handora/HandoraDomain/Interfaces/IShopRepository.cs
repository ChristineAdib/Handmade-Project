using HandoraDomain.Models.ShopEntities;
using System;
using System.Threading.Tasks;

namespace HandoraDomain.Interfaces
{
    public interface IShopRepository : IGenericRepository<Shop, Guid>
    {
        Task<Shop?> GetByIdAsync(Guid id);
        Task<bool> OwnerOwnsShopAsync(string ownerId, Guid shopId);
    }
}
