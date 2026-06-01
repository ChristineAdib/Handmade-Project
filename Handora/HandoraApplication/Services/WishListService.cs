using HandoraApplication.DTOs.WishlistDTOs;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.ProductEntities;
using HandoraDomain.Models.WishListEntoties;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.Services
{
  public class WishListService(IUnitOfWork unitOfWork) : IWishListService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private async Task<WishList> GetOrCreateWishListAsync(string userId)
        {
            var repo = _unitOfWork.Repository<WishList, Guid>();
            var all = await repo.GetAllAsync();

            var wishList = await all
                .Include(w => w.Items.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(w => w.UserId == userId && !w.IsDeleted);

            if (wishList is null)
            {
                wishList = new WishList { UserId = userId };
                await repo.AddAsync(wishList);
                await _unitOfWork.SaveChangesAsync();
            }

            return wishList;
        }
        /// <summary>
        /// //////////////////
        /// </summary>
        /// <returns></returns>
        public async Task<Result<WishListDto>> GetWishListAsync(string userId)
        {
            var wishList = await GetOrCreateWishListAsync(userId);
            return Result<WishListDto>.Success(wishList.Adapt<WishListDto>());
        }



        public async Task<Result<WishListDto>> AddItemAsync(string userId, AddToWishListDto dto)
        {
            var wishList = await GetOrCreateWishListAsync(userId);

            var alreadyExists = wishList.Items
                .Any(i => i.ProductId == dto.ProductId && !i.IsDeleted);

            if (alreadyExists)
                return Result<WishListDto>.Failure("Product already in wishlist");

            var product = await _unitOfWork.Repository<Product, Guid>().GetByIdAsync(dto.ProductId);

            if (product is null || product.IsDeleted)
                return Result<WishListDto>.Failure("Product not found");

            var itemRepo = _unitOfWork.Repository<WishListItem, Guid>();
            await itemRepo.AddAsync(new WishListItem
            {
                WishListId = wishList.Id,
                ProductId = dto.ProductId,
                Quantity = 1
            });

            await _unitOfWork.SaveChangesAsync();

            var updated = await GetOrCreateWishListAsync(userId);
            return Result<WishListDto>.Success(updated.Adapt<WishListDto>());
        }


        public async Task<Result<WishListDto>> RemoveItemAsync(string userId, Guid productId)
        {
            var wishList = await GetOrCreateWishListAsync(userId);

            var item = wishList.Items
                .FirstOrDefault(i => i.ProductId == productId && !i.IsDeleted);

            if (item is null)
                return Result<WishListDto>.Failure("Product not found in wishlist");

            await _unitOfWork.Repository<WishListItem, Guid>().SoftDeleteAsync(item);
            await _unitOfWork.SaveChangesAsync();

            var updated = await GetOrCreateWishListAsync(userId);
            return Result<WishListDto>.Success(updated.Adapt<WishListDto>());
        }
    }
}
