using HandoraApplication.DTOs.CouponDTOs;
using HandoraApplication.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HandoraApplication.IServices
{
    public interface ICouponService
    {
        Task<Result<CouponResponseDto>> CreateCouponAsync(string sellerId, CreateCouponDto dto);
        Task<Result<CouponResponseDto>> UpdateCouponAsync(string sellerId, Guid id, UpdateCouponDto dto);
        Task<Result> DeleteCouponAsync(string sellerId, Guid id);
        Task<Result<IEnumerable<CouponResponseDto>>> GetMyCouponsAsync(string sellerId);
        Task<Result<CouponResultDto>> ApplyCouponAsync(ApplyCouponDto dto);
    }
}
