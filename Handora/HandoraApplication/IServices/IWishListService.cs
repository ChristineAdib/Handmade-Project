using HandoraApplication.DTOs.WishlistDTOs;
using HandoraApplication.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.IServices
{
   public interface IWishListService
    {
        Task<Result<WishListDto>> GetWishListAsync(string userId);
        Task<Result<WishListDto>> AddItemAsync(string userId, AddToWishListDto dto);
        Task<Result<WishListDto>> RemoveItemAsync(string userId, Guid productId);
        Task<Result<WishListDto>> SyncWishListAsync(string userId, List<Guid> productIds);
    }
}
