using AuthenticationDemo.DTOs;
using AuthenticationDemo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthenticationDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name) ?? "0");

        [Authorize(Policy = "RequireAuthenticatedUser")]
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetUserId();
            var cart = await _cartService.GetCartAsync(userId);
            return Ok(cart);
        }

        [Authorize(Policy = "RequireAuthenticatedUser")]
        [HttpPost("add")]
        public async Task<IActionResult> AddItem([FromBody] CartItemDto item)
        {
            var userId = GetUserId();
            await _cartService.AddItemAsync(userId, item);
            return Ok();
        }

        [Authorize(Policy = "RequireAuthenticatedUser")]
        [HttpPost("remove")]
        public async Task<IActionResult> RemoveItem([FromBody] int productId)
        {
            var userId = GetUserId();
            await _cartService.RemoveItemAsync(userId, productId);
            return Ok();
        }

        [Authorize(Policy = "RequireAuthenticatedUser")]
        [HttpPost("update")]
        public async Task<IActionResult> UpdateItem([FromBody] CartItemDto item)
        {
            var userId = GetUserId();
            await _cartService.UpdateItemAsync(userId, item);
            return Ok();
        }

        [Authorize]
        [HttpPost("merge-guest-cart")]
        public async Task<IActionResult> MergeGuestCart([FromBody] string sessionKey)
        {
            var userId = GetUserId();
            await _cartService.MergeGuestCartAsync(userId, sessionKey);
            return Ok();
        }

        // Guest cart endpoints (no auth)
        [HttpGet("guest")] // ?sessionKey=...
        public async Task<IActionResult> GetGuestCart([FromQuery] string sessionKey)
        {
            var cart = await _cartService.GetGuestCartAsync(sessionKey);
            return Ok(cart);
        }

        [HttpPost("guest/add")] // { sessionKey, item }
        public async Task<IActionResult> AddItemToGuestCart([FromBody] GuestCartItemRequest req)
        {
            await _cartService.AddItemToGuestCartAsync(req.SessionKey, req.Item);
            return Ok();
        }

        [HttpPost("guest/remove")] // { sessionKey, productId }
        public async Task<IActionResult> RemoveItemFromGuestCart([FromBody] GuestCartRemoveRequest req)
        {
            await _cartService.RemoveItemFromGuestCartAsync(req.SessionKey, req.ProductId);
            return Ok();
        }

        [HttpPost("guest/update")] // { sessionKey, item }
        public async Task<IActionResult> UpdateItemInGuestCart([FromBody] GuestCartItemRequest req)
        {
            await _cartService.UpdateItemInGuestCartAsync(req.SessionKey, req.Item);
            return Ok();
        }
    }

    public class GuestCartItemRequest
    {
        public string SessionKey { get; set; }
        public CartItemDto Item { get; set; }
    }
    public class GuestCartRemoveRequest
    {
        public string SessionKey { get; set; }
        public int ProductId { get; set; }
    }
} 