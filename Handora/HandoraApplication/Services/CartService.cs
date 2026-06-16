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

            if (cart.Items.Any())
            {
                var productIds = cart.Items.Select(i => i.ProductId).ToList();
                var dbProducts = (await _productRepository.GetProductsByIdsAsync(productIds))
                    .ToDictionary(p => p.Id);

                bool isUpdated = false;
                var itemsToRemove = new List<CartItemDto>();

                foreach (var item in cart.Items)
                {
                    if (dbProducts.TryGetValue(item.ProductId, out var product))
                    {
                        var latestImageUrl = product.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl 
                                             ?? product.Images.FirstOrDefault()?.ImageUrl;

                        var isProductAvailable = product.Quantity > 0 && product.Status == ProductStatus.Active;
                        var stockQuantity = product.Quantity;
                        var isProductSoldOut = product.Quantity <= 0;

                        if (item.TitleEn != product.TitleEn ||
                            item.TitleAr != product.TitleAr ||
                            item.Price != product.Price ||
                            item.DiscountPrice != product.DiscountPrice ||
                            item.ImageUrl != latestImageUrl ||
                            item.IsAvailable != isProductAvailable ||
                            item.StockQuantity != stockQuantity ||
                            item.IsSoldOut != isProductSoldOut)
                        {
                            item.TitleEn = product.TitleEn;
                            item.TitleAr = product.TitleAr;
                            item.Price = product.Price;
                            item.DiscountPrice = product.DiscountPrice;
                            item.ImageUrl = latestImageUrl;
                            item.IsAvailable = isProductAvailable;
                            item.StockQuantity = stockQuantity;
                            item.IsSoldOut = isProductSoldOut;
                            isUpdated = true;
                        }
                    }
                    else
                    {
                        itemsToRemove.Add(item);
                        isUpdated = true;
                    }
                }

                if (itemsToRemove.Any())
                {
                    foreach (var item in itemsToRemove)
                    {
                        cart.Items.Remove(item);
                    }
                }

                if (isUpdated)
                {
                    await SaveCartAsync(cart);
                }
            }

            return Result<CartDto>.Success(cart);
        }

        // ── Add ──────────────────────────────────────────────────────────────
        public async Task<Result<CartDto>> AddItemAsync(string cartId, AddToCartDto dto)
        {
            if (dto.Quantity < 1)
                return Result<CartDto>.Failure("Minimum quantity is 1.");

            var product = await _productRepository.GetProductByIDWithDetailsAsync(dto.ProductId);

            if (product is null || product.IsDeleted)
                return Result<CartDto>.Failure("Product not found");

            if (product.Quantity <= 0 || product.Status != ProductStatus.Active)
                return Result<CartDto>.Failure("This product is currently unavailable.");

            if (cartId.StartsWith("user:"))
            {
                var userId = cartId.Substring("user:".Length);
                if (product.Shop.OwnerId == userId)
                    return Result<CartDto>.Failure("You cannot purchase your own products.");
            }

            if (product.Quantity < dto.Quantity)
                return Result<CartDto>.Failure("Requested quantity exceeds available stock.");

            var cart = await GetOrCreateCartAsync(cartId);

            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);

            if (existingItem is not null)
            {
                var newQuantity = existingItem.Quantity + dto.Quantity;
                if (product.Quantity < newQuantity)
                    return Result<CartDto>.Failure("Requested quantity exceeds available stock.");

                existingItem.Quantity = newQuantity;
                existingItem.IsAvailable = true;
                existingItem.StockQuantity = product.Quantity;
                existingItem.IsSoldOut = false;
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
                    ImageUrl = product.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl ?? product.Images.FirstOrDefault()?.ImageUrl,
                    Quantity = dto.Quantity,
                    IsAvailable = true,
                    StockQuantity = product.Quantity,
                    IsSoldOut = false
                });
            }

            await SaveCartAsync(cart);
            return Result<CartDto>.Success(cart);
        }

        // ── Update Quantity ──────────────────────────────────────────────────
        public async Task<Result<CartDto>> UpdateItemQuantityAsync(string cartId, UpdateCartItemDto dto)
        {
            if (dto.Quantity < 1)
                return Result<CartDto>.Failure("Minimum quantity is 1.");

            var cart = await GetOrCreateCartAsync(cartId);

            var item = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);

            if (item is null)
                return Result<CartDto>.Failure("Item not found in cart");

            var product = await _productRepository.GetProductByIDWithDetailsAsync(dto.ProductId);

            if (product is null || product.IsDeleted)
                return Result<CartDto>.Failure("Product not found");

            if (product.Quantity <= 0 || product.Status != ProductStatus.Active)
                return Result<CartDto>.Failure("This product is currently unavailable.");

            if (product.Quantity < dto.Quantity)
                return Result<CartDto>.Failure("Requested quantity exceeds available stock.");

            item.Quantity = dto.Quantity;
            item.StockQuantity = product.Quantity;
            item.IsAvailable = true;
            item.IsSoldOut = false;

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

        // ── Sync ─────────────────────────────────────────────────────────────
        public async Task<Result<CartDto>> SyncCartAsync(string cartId, List<CartItemDto> guestItems)
        {
            var cart = await GetOrCreateCartAsync(cartId);

            string? userId = null;
            if (cartId.StartsWith("user:"))
                userId = cartId.Substring("user:".Length);

            if (guestItems is not null && guestItems.Any())
            {
                var productIds = guestItems.Select(i => i.ProductId).ToList();
                var dbProducts = (await _productRepository.GetProductsByIdsAsync(productIds))
                    .ToDictionary(p => p.Id);

                foreach (var guestItem in guestItems)
                {
                    if (dbProducts.TryGetValue(guestItem.ProductId, out var product))
                    {
                        if (product.IsDeleted) continue;

                        if (userId is not null && product.Shop.OwnerId == userId)
                            continue;

                        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == guestItem.ProductId);
                        var latestImageUrl = product.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl 
                                             ?? product.Images.FirstOrDefault()?.ImageUrl;

                        if (existingItem is not null)
                        {
                            var newQuantity = existingItem.Quantity + guestItem.Quantity;
                            existingItem.Quantity = Math.Min(newQuantity, product.Quantity);
                            existingItem.TitleEn = product.TitleEn;
                            existingItem.TitleAr = product.TitleAr;
                            existingItem.Price = product.Price;
                            existingItem.DiscountPrice = product.DiscountPrice;
                            existingItem.ImageUrl = latestImageUrl;
                            existingItem.IsAvailable = product.Quantity > 0 && product.Status == ProductStatus.Active;
                            existingItem.StockQuantity = product.Quantity;
                            existingItem.IsSoldOut = product.Quantity <= 0;
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
                                ImageUrl = latestImageUrl,
                                Quantity = Math.Min(guestItem.Quantity, product.Quantity),
                                IsAvailable = product.Quantity > 0 && product.Status == ProductStatus.Active,
                                StockQuantity = product.Quantity,
                                IsSoldOut = product.Quantity <= 0
                            });
                        }
                    }
                }

                await SaveCartAsync(cart);
            }

            return Result<CartDto>.Success(cart);
        }
    }
}
