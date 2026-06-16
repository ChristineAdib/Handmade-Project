using HandoraApplication.DTOs.CartDTOs;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.CartEntities;
using HandoraDomain.Models.ProductEntities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;
using System.Text.Json;

namespace HandoraApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrderRepository _orderRepository;
        private readonly IDistributedCache _cache;

        public CartController(ICartService cartService, IUnitOfWork unitOfWork,
            IOrderRepository orderRepository, IDistributedCache cache)
        {
            _cartService = cartService;
            _unitOfWork = unitOfWork;
            _orderRepository = orderRepository;
            _cache = cache;
        }

        private static string GetMemoryCartKey(string cartId) => $"cart:{cartId}";

        private async Task LoadDbCartToMemoryAsync(string memoryCartId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return;

            var dbCart = await _orderRepository.GetUserCartWithItemsAsync(userId);
            if (dbCart is null || dbCart.Items.Count == 0)
                return;

            var dto = new CartDto { CartId = memoryCartId };
            foreach (var item in dbCart.Items)
            {
                var unitPrice = item.Product.DiscountPrice ?? item.Product.Price;
                dto.Items.Add(new CartItemDto
                {
                    ProductId = item.ProductId,
                    TitleEn = item.Product.TitleEn,
                    TitleAr = item.Product.TitleAr,
                    Price = item.Product.Price,
                    DiscountPrice = item.Product.DiscountPrice,
                    ImageUrl = item.Product.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl ?? item.Product.Images.FirstOrDefault()?.ImageUrl,
                    Quantity = item.Quantity
                });
            }

            var json = JsonSerializer.Serialize(dto);
            await _cache.SetStringAsync(GetMemoryCartKey(memoryCartId), json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
            });
        }

        private string GetOrCreateCartId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
                return $"user:{userId}";

            var cartId = Request.Cookies["cartId"];

            if (string.IsNullOrEmpty(cartId))
            {
                cartId = Guid.NewGuid().ToString();
                Response.Cookies.Append("cartId", cartId, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(7),
                    HttpOnly = true,
                    IsEssential = true
                });
            }

            return $"cookie:{cartId}";
        }

        private async Task SyncRedisCartToDbCartAsync(string cartId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return;

            var redisCart = await _cartService.GetCartAsync(cartId);
            if (!redisCart.IsSuccess || redisCart.Data is null)
                return;

            var cartRepo = _unitOfWork.Repository<Cart, Guid>();
            var cartItemRepo = _unitOfWork.Repository<CartItem, Guid>();
            var productRepo = _unitOfWork.Repository<Product, Guid>();

            var dbCart = await (await cartRepo.GetAllAsync())
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (dbCart is null)
            {
                dbCart = new Cart
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                await cartRepo.AddAsync(dbCart);
                await _unitOfWork.SaveChangesAsync();
            }

            var existingItems = await (await cartItemRepo.GetAllAsync())
                .Where(i => i.CartId == dbCart.Id && !i.IsDeleted)
                .ToListAsync();

            foreach (var item in existingItems)
                await cartItemRepo.HardDeleteAsync(item);

            foreach (var redisItem in redisCart.Data.Items)
            {
                var product = await productRepo.GetByIdAsync(redisItem.ProductId);
                var unitPrice = product?.DiscountPrice ?? redisItem.Price;

                var newItem = new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = dbCart.Id,
                    ProductId = redisItem.ProductId,
                    Quantity = redisItem.Quantity,
                    TotalPrice = unitPrice * redisItem.Quantity,
                    CreatedAt = DateTime.UtcNow
                };
                await cartItemRepo.AddAsync(newItem);
            }

            dbCart.UpdatedAt = DateTime.UtcNow;
            await cartRepo.UpdateAsync(dbCart);
            await _unitOfWork.SaveChangesAsync();
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var cartId = GetOrCreateCartId();
            var result = await _cartService.GetCartAsync(cartId);

            if (result.IsSuccess && result.Data is { Items.Count: 0 })
            {
                await LoadDbCartToMemoryAsync(cartId);
                result = await _cartService.GetCartAsync(cartId);
            }

            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }

        [HttpPost]
        public async Task<IActionResult> AddItem([FromBody] AddToCartDto dto)
        {
            var result = await _cartService.AddItemAsync(GetOrCreateCartId(), dto);
            if (result.IsSuccess)
                await SyncRedisCartToDbCartAsync(GetOrCreateCartId());
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateItem([FromBody] UpdateCartItemDto dto)
        {
            var result = await _cartService.UpdateItemQuantityAsync(GetOrCreateCartId(), dto);
            if (result.IsSuccess)
                await SyncRedisCartToDbCartAsync(GetOrCreateCartId());
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }

        [HttpDelete("{productId:guid}")]
        public async Task<IActionResult> RemoveItem(Guid productId)
        {
            var result = await _cartService.RemoveItemAsync(GetOrCreateCartId(), productId);
            if (result.IsSuccess)
                await SyncRedisCartToDbCartAsync(GetOrCreateCartId());
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var result = await _cartService.ClearCartAsync(GetOrCreateCartId());
            if (result.IsSuccess)
                await SyncRedisCartToDbCartAsync(GetOrCreateCartId());
            return result.IsSuccess ? Ok("Cart cleared successfully.") : BadRequest(result.Errors);
        }

        [HttpPost("sync")]
        [Authorize]
        public async Task<IActionResult> SyncGuestCart([FromBody] List<CartItemDto> guestItems)
        {
            var cartId = GetOrCreateCartId();
            var result = await _cartService.SyncCartAsync(cartId, guestItems);
            if (result.IsSuccess)
                await SyncRedisCartToDbCartAsync(cartId);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }
    }
}
