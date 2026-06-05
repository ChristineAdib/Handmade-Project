using HandoraDomain.Interfaces;
using HandoraDomain.Models.CouponEntities;
using HandoraInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HandoraInfrastructure.Repositries
{
    public class CouponRepository(AppDbContext context)
        : GenericRepository<Coupon, Guid>(context), ICouponRepository
    {
        private readonly AppDbContext _context = context;

        public async Task<Coupon?> GetByCodeAsync(string code)
        {
            var normalizedCode = code.Trim().ToUpper();
            return await _context.Coupons
                .Include(c => c.Seller)
                .FirstOrDefaultAsync(c => c.Code.ToUpper() == normalizedCode && !c.IsDeleted);
        }

        public async Task<Coupon?> GetByIdWithSellerAsync(Guid id)
        {
            return await _context.Coupons
                .Include(c => c.Seller)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        }

        public async Task<Coupon?> GetByIdWithSellerAndOrdersAsync(Guid id)
        {
            return await _context.Coupons
                .Include(c => c.Seller)
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        }

        public async Task<IEnumerable<Coupon>> GetBySellerAsync(string sellerId)
        {
            return await _context.Coupons
                .Include(c => c.Seller)
                .Where(c => c.SellerId == sellerId && !c.IsDeleted)
                .ToListAsync();
        }

        public async Task<bool> CodeExistsAsync(string code)
        {
            var normalizedCode = code.Trim().ToUpper();
            return await _context.Coupons
                .AnyAsync(c => c.Code.ToUpper() == normalizedCode && !c.IsDeleted);
        }
    }
}
