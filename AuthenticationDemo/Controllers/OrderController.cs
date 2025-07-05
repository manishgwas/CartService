using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthenticationDemo.Services;
using AuthenticationDemo.DTOs;

namespace AuthenticationDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name) ?? "0");

        [Authorize(Policy = "RequireAuthenticatedUser")]
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] OrderRequestDto dto)
        {
            var userId = GetUserId();
            var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                return BadRequest(new { error = "Idempotency-Key header is required" });
            try
            {
                var result = await _orderService.CheckoutAsync(userId, dto, idempotencyKey);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
} 