using System.Collections.Generic;
using System.Threading.Tasks;
using AuthenticationDemo.DTOs;

namespace AuthenticationDemo.Services
{
    public interface IPaymentService
    {
        Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentRequestDto request, string userId);
        Task<PaymentResponseDto> GetPaymentByIdAsync(int id);
        Task<PaymentResponseDto> GetPaymentByRazorpayIdAsync(string razorpayPaymentId);
        Task<IEnumerable<PaymentResponseDto>> GetPaymentsByUserIdAsync(string userId);
        Task<IEnumerable<PaymentResponseDto>> GetPaymentsByOrderIdAsync(int orderId);
        Task<PaymentResponseDto> CapturePaymentAsync(CapturePaymentRequestDto request);
        Task<PaymentResponseDto> RefundPaymentAsync(RefundPaymentRequestDto request);
        Task<bool> VerifyPaymentSignatureAsync(PaymentVerificationDto request);
        Task<RazorpayOrderResponseDto> CreateRazorpayOrderAsync(CreatePaymentRequestDto request);
        Task<IEnumerable<PaymentResponseDto>> GetAllPaymentsAsync();
        Task<IEnumerable<PaymentResponseDto>> GetPaymentsByStatusAsync(string status);
        Task<bool> ProcessWebhookAsync(string requestBody, string signature);
    }
} 