using HandoraDomain.Interfaces;
using HandoraDomain.Models.ShopEntities;
using HandoraInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace HandoraInfrastructure.Repositries
{
    public class ShopRepository(AppDbContext context)
        : GenericRepository<Shop, Guid>(context), IShopRepository
    {
        private readonly AppDbContext _context = context;

        public async Task<Shop?> GetByIdAsync(Guid id)
        {
            return await _context.Shops
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        }

        public async Task<bool> OwnerOwnsShopAsync(string ownerId, Guid shopId)
        {
            return await _context.Shops
                .AnyAsync(s => s.Id == shopId && s.OwnerId == ownerId && !s.IsDeleted);
        }
    }
}
