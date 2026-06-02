using HandoraDomain.Models.CouponEntities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HandoraDomain.Interfaces
{
    public interface ICouponRepository : IGenericRepository<Coupon, Guid>
    {
        Task<Coupon?> GetByCodeAsync(string code);
        Task<Coupon?> GetByIdWithSellerAsync(Guid id);
        Task<Coupon?> GetByIdWithSellerAndOrdersAsync(Guid id);
        Task<IEnumerable<Coupon>> GetBySellerAsync(string sellerId);
        Task<bool> CodeExistsAsync(string code);
    }
}
