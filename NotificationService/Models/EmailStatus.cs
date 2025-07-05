using System;

namespace NotificationService.Models
{
    public class EmailStatus
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; }
        public string Status { get; set; } // Pending, Sent, Failed
        public int RetryCount { get; set; }
        public DateTime? LastTriedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public string ErrorMessage { get; set; }
    }
} 