using AuthenticationDemo.DTOs;
using AuthenticationDemo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System;
using System.IO;
using AuthenticationDemo.Attributes;

namespace AuthenticationDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("create")]
        [SlidingWindowRateLimit(10, 60, "userid")] // 10 requests per minute per user
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var result = await _paymentService.CreatePaymentAsync(request, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [SlidingWindowRateLimit(20, 60, "userid")] // 20 requests per minute per user
        public async Task<IActionResult> GetPayment(int id)
        {
            try
            {
                var result = await _paymentService.GetPaymentByIdAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("razorpay/{razorpayPaymentId}")]
        [SlidingWindowRateLimit(20, 60, "userid")] // 20 requests per minute per user
        public async Task<IActionResult> GetPaymentByRazorpayId(string razorpayPaymentId)
        {
            try
            {
                var result = await _paymentService.GetPaymentByRazorpayIdAsync(razorpayPaymentId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("user")]
        [SlidingWindowRateLimit(30, 60, "userid")] // 30 requests per minute per user
        public async Task<IActionResult> GetUserPayments()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var result = await _paymentService.GetPaymentsByUserIdAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("order/{orderId}")]
        [SlidingWindowRateLimit(20, 60, "userid")] // 20 requests per minute per user
        public async Task<IActionResult> GetOrderPayments(int orderId)
        {
            try
            {
                var result = await _paymentService.GetPaymentsByOrderIdAsync(orderId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("capture")]
        [Authorize(Roles = "Admin")]
        [SlidingWindowRateLimit(5, 60, "userid")] // 5 requests per minute per admin user
        public async Task<IActionResult> CapturePayment([FromBody] CapturePaymentRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _paymentService.CapturePaymentAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("refund")]
        [Authorize(Roles = "Admin")]
        [SlidingWindowRateLimit(5, 60, "userid")] // 5 requests per minute per admin user
        public async Task<IActionResult> RefundPayment([FromBody] RefundPaymentRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _paymentService.RefundPaymentAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("verify")]
        [SlidingWindowRateLimit(10, 60, "userid")] // 10 requests per minute per user
        public async Task<IActionResult> VerifyPayment([FromBody] PaymentVerificationDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var isValid = await _paymentService.VerifyPaymentSignatureAsync(request);
                return Ok(new { isValid });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        [SlidingWindowRateLimit(10, 60, "userid")] // 10 requests per minute per admin user
        public async Task<IActionResult> GetAllPayments()
        {
            try
            {
                var result = await _paymentService.GetAllPaymentsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin")]
        [SlidingWindowRateLimit(10, 60, "userid")] // 10 requests per minute per admin user
        public async Task<IActionResult> GetPaymentsByStatus(string status)
        {
            try
            {
                var result = await _paymentService.GetPaymentsByStatusAsync(status);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        [SlidingWindowRateLimit(50, 60, "ip")] // 50 requests per minute per IP for webhooks
        public async Task<IActionResult> Webhook()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var payload = await reader.ReadToEndAsync();
                
                var result = await _paymentService.ProcessWebhookAsync(payload, Request.Headers);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
} 