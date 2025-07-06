using AuthenticationDemo.DTOs;
using AuthenticationDemo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System;
using System.IO;

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
        public async Task<IActionResult> PaymentWebhook()
        {
            try
            {
                // Read the request body
                using var reader = new StreamReader(Request.Body);
                var requestBody = await reader.ReadToEndAsync();
                
                // Get the signature from headers
                var signature = Request.Headers["X-Razorpay-Signature"].ToString();
                
                if (string.IsNullOrEmpty(signature))
                {
                    return BadRequest(new { error = "Missing webhook signature" });
                }
                
                // Process the webhook
                var success = await _paymentService.ProcessWebhookAsync(requestBody, signature);
                
                if (success)
                {
                    return Ok(new { message = "Webhook processed successfully" });
                }
                else
                {
                    return BadRequest(new { error = "Failed to process webhook" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
} 