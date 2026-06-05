using HandoraApplication.DTOs.CouponDTOs;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.CouponEntities;
using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HandoraApplication.Services
{
    public class CouponService(
        IUnitOfWork unitOfWork,
        ICouponRepository couponRepository) : ICouponService
    {
        private readonly IUnitOfWork _uow = unitOfWork;
        private readonly ICouponRepository _couponRepository = couponRepository;

        public async Task<Result<CouponResponseDto>> CreateCouponAsync(string sellerId, CreateCouponDto dto)
        {
            var normalizedCode = dto.Code.Trim().ToUpper();
            var codeExists = await _couponRepository.CodeExistsAsync(normalizedCode);
            if (codeExists)
                return Result<CouponResponseDto>.Failure($"Coupon code '{dto.Code}' already exists.");

            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = normalizedCode,
                DiscountValue = dto.DiscountValue,
                DiscountType = dto.DiscountType,
                ExpiryDate = dto.ExpiryDate,
                MinOrderValue = dto.MinOrderValue,
                MaxUsageCount = dto.MaxUsageCount,
                CurrentUsageCount = 0,
                IsActive = true,
                SellerId = sellerId
            };
            coupon.CreatedAt = DateTime.UtcNow;
            coupon.CreatedBy = sellerId;

            await _couponRepository.AddAsync(coupon);
            await _uow.SaveChangesAsync();

            var response = coupon.Adapt<CouponResponseDto>();
            return Result<CouponResponseDto>.Success(response);
        }

        public async Task<Result<CouponResponseDto>> UpdateCouponAsync(string sellerId, Guid id, UpdateCouponDto dto)
        {
            var coupon = await _couponRepository.GetByIdWithSellerAsync(id);
            if (coupon is null)
                return Result<CouponResponseDto>.Failure("Coupon not found.");

            if (coupon.SellerId != sellerId)
                return Result<CouponResponseDto>.Failure("Unauthorized. You do not own this coupon.");

            coupon.DiscountType = dto.DiscountType;
            coupon.DiscountValue = dto.DiscountValue;
            coupon.MinOrderValue = dto.MinOrderValue;
            coupon.ExpiryDate = dto.ExpiryDate;
            coupon.MaxUsageCount = dto.MaxUsageCount;
            coupon.IsActive = dto.IsActive;
            coupon.UpdatedAt = DateTime.UtcNow;
            coupon.UpdatedBy = sellerId;

            await _couponRepository.UpdateAsync(coupon);
            await _uow.SaveChangesAsync();

            var response = coupon.Adapt<CouponResponseDto>();
            return Result<CouponResponseDto>.Success(response);
        }

        public async Task<Result> DeleteCouponAsync(string sellerId, Guid id)
        {
            var coupon = await _couponRepository.GetByIdWithSellerAndOrdersAsync(id);
            if (coupon is null)
                return Result.Failure("Coupon not found.");

            if (coupon.SellerId != sellerId)
                return Result.Failure("Unauthorized. You do not own this coupon.");

            if (coupon.Orders != null && coupon.Orders.Any())
            {
                await _couponRepository.SoftDeleteAsync(coupon);
            }
            else
            {
                await _couponRepository.HardDeleteAsync(coupon);
            }

            await _uow.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result<IEnumerable<CouponResponseDto>>> GetMyCouponsAsync(string sellerId)
        {
            var coupons = await _couponRepository.GetBySellerAsync(sellerId);
            var response = coupons.Select(c => c.Adapt<CouponResponseDto>());
            return Result<IEnumerable<CouponResponseDto>>.Success(response);
        }

        public async Task<Result<CouponResultDto>> ApplyCouponAsync(ApplyCouponDto dto)
        {
            var coupon = await _couponRepository.GetByCodeAsync(dto.Code);
            if (coupon is null)
                return Result<CouponResultDto>.Failure("Coupon code not found.");

            if (!coupon.IsActive)
                return Result<CouponResultDto>.Failure("Coupon is inactive.");

            if (DateTime.UtcNow > coupon.ExpiryDate)
                return Result<CouponResultDto>.Failure("Coupon has expired.");

            if (coupon.MaxUsageCount.HasValue && coupon.CurrentUsageCount >= coupon.MaxUsageCount.Value)
                return Result<CouponResultDto>.Failure("Coupon max usage reached.");

            if (coupon.SellerId != dto.SellerId)
                return Result<CouponResultDto>.Failure("Coupon from different seller.");

            if (coupon.MinOrderValue.HasValue && dto.OrderTotal < coupon.MinOrderValue.Value)
                return Result<CouponResultDto>.Failure("Order total is below the minimum order value of " + coupon.MinOrderValue.Value + " required for this coupon.");

            decimal discountAmount = 0;
            if (coupon.DiscountType == DiscountType.Percentage)
            {
                discountAmount = dto.OrderTotal * (coupon.DiscountValue / 100m);
            }
            else if (coupon.DiscountType == DiscountType.FixedAmount)
            {
                discountAmount = coupon.DiscountValue;
                if (discountAmount > dto.OrderTotal)
                {
                    discountAmount = dto.OrderTotal;
                }
            }

            discountAmount = Math.Round(discountAmount, 2);

            var result = new CouponResultDto
            {
                IsValid = true,
                DiscountAmount = discountAmount,
                Message = "Coupon applied successfully."
            };

            return Result<CouponResultDto>.Success(result);
        }
    }
}
