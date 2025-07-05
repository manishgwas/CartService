using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthenticationDemo.DTOs;
using AuthenticationDemo.Models;
using AuthenticationDemo.Repositories;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AuthenticationDemo.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _razorpayKeyId;
        private readonly string _razorpayKeySecret;
        private readonly string _razorpayBaseUrl = "https://api.razorpay.com/v1";

        public PaymentService(
            IPaymentRepository paymentRepository,
            IOrderRepository orderRepository,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
            _configuration = configuration;
            _httpClient = httpClient;
            
            _razorpayKeyId = _configuration["Razorpay:KeyId"];
            _razorpayKeySecret = _configuration["Razorpay:KeySecret"];
            
            // Set up basic auth for Razorpay API
            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_razorpayKeyId}:{_razorpayKeySecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);
        }

        public async Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentRequestDto request, string userId)
        {
            // Verify order exists and belongs to user
            var order = await _orderRepository.GetByIdAsync(request.OrderId);
            if (order == null)
                throw new InvalidOperationException("Order not found");
            
            if (order.UserId != userId)
                throw new UnauthorizedAccessException("Order does not belong to user");

            // Create Razorpay order
            var razorpayOrder = await CreateRazorpayOrderAsync(request);
            
            // Create payment record
            var payment = new Payment
            {
                UserId = userId,
                OrderId = request.OrderId,
                RazorpayOrderId = razorpayOrder.Id,
                RazorpayPaymentId = "", // Will be set when payment is captured
                Amount = request.Amount,
                Currency = request.Currency,
                Status = "pending",
                Description = request.Description,
                Receipt = request.Receipt ?? $"receipt_{DateTime.UtcNow.Ticks}",
                Email = request.Email,
                Contact = request.Contact
            };

            var createdPayment = await _paymentRepository.CreateAsync(payment);
            return MapToPaymentResponseDto(createdPayment);
        }

        public async Task<PaymentResponseDto> GetPaymentByIdAsync(int id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null)
                throw new InvalidOperationException("Payment not found");
            
            return MapToPaymentResponseDto(payment);
        }

        public async Task<PaymentResponseDto> GetPaymentByRazorpayIdAsync(string razorpayPaymentId)
        {
            var payment = await _paymentRepository.GetByRazorpayPaymentIdAsync(razorpayPaymentId);
            if (payment == null)
                throw new InvalidOperationException("Payment not found");
            
            return MapToPaymentResponseDto(payment);
        }

        public async Task<IEnumerable<PaymentResponseDto>> GetPaymentsByUserIdAsync(string userId)
        {
            var payments = await _paymentRepository.GetByUserIdAsync(userId);
            return payments.Select(MapToPaymentResponseDto);
        }

        public async Task<IEnumerable<PaymentResponseDto>> GetPaymentsByOrderIdAsync(string orderId)
        {
            var payments = await _paymentRepository.GetByOrderIdAsync(orderId);
            return payments.Select(MapToPaymentResponseDto);
        }

        public async Task<PaymentResponseDto> CapturePaymentAsync(CapturePaymentRequestDto request)
        {
            var payment = await _paymentRepository.GetByRazorpayPaymentIdAsync(request.PaymentId);
            if (payment == null)
                throw new InvalidOperationException("Payment not found");

            if (payment.Status != "pending")
                throw new InvalidOperationException("Payment is not in pending status");

            // Capture payment with Razorpay
            var captureData = new
            {
                amount = (int)(request.Amount * 100), // Razorpay expects amount in paise
                currency = request.Currency
            };

            var json = JsonSerializer.Serialize(captureData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_razorpayBaseUrl}/payments/{request.PaymentId}/capture", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to capture payment: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var captureResponse = JsonSerializer.Deserialize<RazorpayCaptureResponse>(responseContent);

            // Update payment record
            payment.Status = "captured";
            payment.RazorpayPaymentId = request.PaymentId;
            payment.CapturedAt = DateTime.UtcNow;
            payment.Method = captureResponse?.Method;
            payment.Email = captureResponse?.Email;
            payment.Contact = captureResponse?.Contact;

            var updatedPayment = await _paymentRepository.UpdateAsync(payment);
            return MapToPaymentResponseDto(updatedPayment);
        }

        public async Task<PaymentResponseDto> RefundPaymentAsync(RefundPaymentRequestDto request)
        {
            var payment = await _paymentRepository.GetByRazorpayPaymentIdAsync(request.PaymentId);
            if (payment == null)
                throw new InvalidOperationException("Payment not found");

            if (payment.Status != "captured")
                throw new InvalidOperationException("Payment is not captured");

            // Create refund with Razorpay
            var refundData = new
            {
                amount = (int)(request.Amount * 100), // Razorpay expects amount in paise
                reason = request.Reason ?? "customer_requested",
                speed = request.Speed ?? "normal"
            };

            var json = JsonSerializer.Serialize(refundData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_razorpayBaseUrl}/payments/{request.PaymentId}/refund", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to refund payment: {errorContent}");
            }

            // Update payment status
            payment.Status = "refunded";
            var updatedPayment = await _paymentRepository.UpdateAsync(payment);
            return MapToPaymentResponseDto(updatedPayment);
        }

        public async Task<bool> VerifyPaymentSignatureAsync(PaymentVerificationDto request)
        {
            var expectedSignature = request.RazorpayOrderId + "|" + request.RazorpayPaymentId;
            var signature = ComputeHmacSha256(expectedSignature, _razorpayKeySecret);
            
            return signature == request.RazorpaySignature;
        }

        public async Task<RazorpayOrderResponseDto> CreateRazorpayOrderAsync(CreatePaymentRequestDto request)
        {
            var orderData = new
            {
                amount = (int)(request.Amount * 100), // Razorpay expects amount in paise
                currency = request.Currency,
                receipt = request.Receipt ?? $"receipt_{DateTime.UtcNow.Ticks}",
                notes = new { description = request.Description }
            };

            var json = JsonSerializer.Serialize(orderData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_razorpayBaseUrl}/orders", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to create Razorpay order: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RazorpayOrderResponseDto>(responseContent);
        }

        public async Task<IEnumerable<PaymentResponseDto>> GetAllPaymentsAsync()
        {
            var payments = await _paymentRepository.GetAllAsync();
            return payments.Select(MapToPaymentResponseDto);
        }

        public async Task<IEnumerable<PaymentResponseDto>> GetPaymentsByStatusAsync(string status)
        {
            var payments = await _paymentRepository.GetByStatusAsync(status);
            return payments.Select(MapToPaymentResponseDto);
        }

        private PaymentResponseDto MapToPaymentResponseDto(Payment payment)
        {
            return new PaymentResponseDto
            {
                Id = payment.Id,
                UserId = payment.UserId,
                OrderId = payment.OrderId,
                RazorpayPaymentId = payment.RazorpayPaymentId,
                RazorpayOrderId = payment.RazorpayOrderId,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Status = payment.Status,
                Description = payment.Description,
                Receipt = payment.Receipt,
                Method = payment.Method,
                Email = payment.Email,
                Contact = payment.Contact,
                CreatedAt = payment.CreatedAt,
                CapturedAt = payment.CapturedAt
            };
        }

        private string ComputeHmacSha256(string message, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return Convert.ToHexString(hash).ToLower();
        }
    }

    public class RazorpayCaptureResponse
    {
        [JsonPropertyName("method")]
        public string Method { get; set; }
        
        [JsonPropertyName("email")]
        public string Email { get; set; }
        
        [JsonPropertyName("contact")]
        public string Contact { get; set; }
    }
} 