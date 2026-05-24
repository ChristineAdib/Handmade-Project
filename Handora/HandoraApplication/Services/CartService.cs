using HandoraApplication.DTOs.CartDTOs;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.ProductEntities;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HandoraApplication.Services
{
    public class CartService(IDistributedCache cache, IUnitOfWork unitOfWork, IProductRepository productRepository) : ICartService
    {
        private readonly IDistributedCache _cache = cache;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IProductRepository _productRepository = productRepository;

        private static string GetKey(string cartId) => $"cart:{cartId}";

        private static DistributedCacheEntryOptions CacheOptions => new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
        };

        // ── helper: get cart from redis ──────────────────────────────────────
        private async Task<CartDto> GetOrCreateCartAsync(string cartId)
        {
            var json = await _cache.GetStringAsync(GetKey(cartId));

            if (json is null)
                return new CartDto { CartId = cartId };

            return JsonSerializer.Deserialize<CartDto>(json)!;
        }

        // ── helper: save cart to redis ───────────────────────────────────────
        private async Task SaveCartAsync(CartDto cart)
        {
            var json = JsonSerializer.Serialize(cart);
            await _cache.SetStringAsync(GetKey(cart.CartId), json, CacheOptions);
        }

        // ── Get ──────────────────────────────────────────────────────────────
        public async Task<Result<CartDto>> GetCartAsync(string cartId)
        {
            var cart = await GetOrCreateCartAsync(cartId);
            return Result<CartDto>.Success(cart);
        }

        // ── Add ──────────────────────────────────────────────────────────────
        public async Task<Result<CartDto>> AddItemAsync(string cartId, AddToCartDto dto)
        {
            if (dto.Quantity <= 0)
                return Result<CartDto>.Failure("Quantity must be greater than 0");

            var product = await _productRepository.GetProductByIDWithDetailsAsync(dto.ProductId);

            if (product is null || product.IsDeleted)
                return Result<CartDto>.Failure("Product not found");

            if (product.Quantity < dto.Quantity)
                return Result<CartDto>.Failure("Not enough stock");

            var cart = await GetOrCreateCartAsync(cartId);

            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);

            if (existingItem is not null)
            {
                existingItem.Quantity += dto.Quantity;
            }
            else
            {
                cart.Items.Add(new CartItemDto
                {
                    ProductId = product.Id,
                    TitleEn = product.TitleEn,
                    TitleAr = product.TitleAr,
                    Price = product.Price,
                    DiscountPrice = product.DiscountPrice,
                    ImageUrl = product.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl,
                    Quantity = dto.Quantity
                });
            }

            await SaveCartAsync(cart);
            return Result<CartDto>.Success(cart);
        }

        // ── Update Quantity ──────────────────────────────────────────────────
        public async Task<Result<CartDto>> UpdateItemQuantityAsync(string cartId, UpdateCartItemDto dto)
        {
            if (dto.Quantity <= 0)
                return Result<CartDto>.Failure("Quantity must be greater than 0");

            var cart = await GetOrCreateCartAsync(cartId);

            var item = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);

            if (item is null)
                return Result<CartDto>.Failure("Item not found in cart");

            var product = await _productRepository.GetProductByIDWithDetailsAsync(dto.ProductId);

            if (product!.Quantity < dto.Quantity)
                return Result<CartDto>.Failure("Not enough stock");

            item.Quantity = dto.Quantity;

            await SaveCartAsync(cart);
            return Result<CartDto>.Success(cart);
        }

        // ── Remove ───────────────────────────────────────────────────────────
        public async Task<Result<CartDto>> RemoveItemAsync(string cartId, Guid productId)
        {
            var cart = await GetOrCreateCartAsync(cartId);

            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (item is null)
                return Result<CartDto>.Failure("Item not found in cart");

            cart.Items.Remove(item);

            await SaveCartAsync(cart);
            return Result<CartDto>.Success(cart);
        }

        // ── Clear ────────────────────────────────────────────────────────────
        public async Task<Result> ClearCartAsync(string cartId)
        {
            var cart = await GetOrCreateCartAsync(cartId);

            if (!cart.Items.Any())
                return Result.Failure("Cart is already empty");

            cart.Items.Clear();

            await SaveCartAsync(cart);
            return Result.Success();
        }
    }

}
