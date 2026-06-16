using HandoraApplication.DTOs.CartDTOs;
using HandoraApplication.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.IServices
{
    public interface ICartService
    {
        Task<Result<CartDto>> GetCartAsync(string cartId);
        Task<Result<CartDto>> AddItemAsync(string cartId, AddToCartDto dto);
        Task<Result<CartDto>> UpdateItemQuantityAsync(string cartId, UpdateCartItemDto dto);
        Task<Result<CartDto>> RemoveItemAsync(string cartId, Guid productId);
        Task<Result> ClearCartAsync(string cartId);
        Task<Result<CartDto>> SyncCartAsync(string cartId, List<CartItemDto> guestItems);
    }
}
