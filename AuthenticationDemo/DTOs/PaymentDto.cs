using System;
using System.ComponentModel.DataAnnotations;

namespace AuthenticationDemo.DTOs
{
    public class CreatePaymentRequestDto
    {
        [Required]
        public string OrderId { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
        
        public string Currency { get; set; } = "INR";
        
        public string? Description { get; set; }
        
        public string? Receipt { get; set; }
        
        public string? Email { get; set; }
        
        public string? Contact { get; set; }
    }
    
    public class PaymentResponseDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string OrderId { get; set; }
        public string RazorpayPaymentId { get; set; }
        public string RazorpayOrderId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string? Description { get; set; }
        public string? Receipt { get; set; }
        public string? Method { get; set; }
        public string? Email { get; set; }
        public string? Contact { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CapturedAt { get; set; }
    }
    
    public class RazorpayOrderResponseDto
    {
        public string Id { get; set; }
        public string Entity { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Receipt { get; set; }
        public string Status { get; set; }
        public int CreatedAt { get; set; }
    }
    
    public class CapturePaymentRequestDto
    {
        [Required]
        public string PaymentId { get; set; }
        
        [Required]
        public decimal Amount { get; set; }
        
        public string Currency { get; set; } = "INR";
    }
    
    public class RefundPaymentRequestDto
    {
        [Required]
        public string PaymentId { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
        
        public string? Reason { get; set; }
        
        public string? Speed { get; set; } // normal, instant
    }
    
    public class PaymentVerificationDto
    {
        [Required]
        public string RazorpayPaymentId { get; set; }
        
        [Required]
        public string RazorpayOrderId { get; set; }
        
        [Required]
        public string RazorpaySignature { get; set; }
    }
} 