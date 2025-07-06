using System;
using System.ComponentModel.DataAnnotations;

namespace AuthenticationDemo.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        public int OrderId { get; set; }
        
        [Required]
        public string RazorpayPaymentId { get; set; }
        
        [Required]
        public string RazorpayOrderId { get; set; }
        
        [Required]
        public decimal Amount { get; set; }
        
        [Required]
        public string Currency { get; set; } = "INR";
        
        [Required]
        public string Status { get; set; } // pending, captured, failed, refunded
        
        public string? Description { get; set; }
        
        public string? Receipt { get; set; }
        
        public string? Method { get; set; } // card, netbanking, upi, etc.
        
        public string? CardId { get; set; }
        
        public string? Bank { get; set; }
        
        public string? Wallet { get; set; }
        
        public string? Vpa { get; set; } // Virtual Payment Address for UPI
        
        public string? Email { get; set; }
        
        public string? Contact { get; set; }
        
        public string? ErrorCode { get; set; }
        
        public string? ErrorDescription { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public DateTime? CapturedAt { get; set; }
        
        // Navigation properties
        public virtual User User { get; set; }
        public virtual Order Order { get; set; }
    }
} 