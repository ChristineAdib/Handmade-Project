using HandoraApplication.DTOs.CartDTOs;
using HandoraApplication.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HandoraApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController(ICartService cartService) : ControllerBase
    {
        private readonly ICartService _cartService = cartService;

        private string GetOrCreateCartId()
        {
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

            return cartId;
        }

        // GET api/cart
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var result = await _cartService.GetCartAsync(GetOrCreateCartId());
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }

        // POST api/cart
        [HttpPost]
        public async Task<IActionResult> AddItem([FromBody] AddToCartDto dto)
        {
            var result = await _cartService.AddItemAsync(GetOrCreateCartId(), dto);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }

        // PUT api/cart
        [HttpPut]
        public async Task<IActionResult> UpdateItem([FromBody] UpdateCartItemDto dto)
        {
            var result = await _cartService.UpdateItemQuantityAsync(GetOrCreateCartId(), dto);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }

        // DELETE api/cart/{productId}
        [HttpDelete("{productId:guid}")]
        public async Task<IActionResult> RemoveItem(Guid productId)
        {
            var result = await _cartService.RemoveItemAsync(GetOrCreateCartId(), productId);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }

        // DELETE api/cart
        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var result = await _cartService.ClearCartAsync(GetOrCreateCartId());
            return result.IsSuccess ? Ok("Cart cleared successfully.") : BadRequest(result.Errors);
        }
    }
}
