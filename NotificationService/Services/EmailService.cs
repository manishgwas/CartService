using System;
using System.Net.Mail;
using System.Threading.Tasks;
using NotificationService.Models;
using NotificationService.Data;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Services
{
    public class EmailService
    {
        private readonly NotificationDbContext _db;
        public EmailService(NotificationDbContext db)
        {
            _db = db;
        }

        public async Task SendOrderConfirmationAsync(OrderPlacedEvent orderEvent, string email)
        {
            var status = await _db.EmailStatuses.FirstOrDefaultAsync(e => e.OrderId == orderEvent.OrderId && e.Email == email);
            if (status == null)
            {
                status = new EmailStatus
                {
                    OrderId = orderEvent.OrderId,
                    UserId = orderEvent.UserId,
                    Email = email,
                    Status = "Pending",
                    RetryCount = 0
                };
                _db.EmailStatuses.Add(status);
                await _db.SaveChangesAsync();
            }

            try
            {
                // Replace with your SMTP/SendGrid logic
                using var smtp = new SmtpClient("localhost"); // Configure as needed
                var mail = new MailMessage("no-reply@demo.com", email)
                {
                    Subject = $"Order Confirmation #{orderEvent.OrderId}",
                    Body = $"Thank you for your order! Total: {orderEvent.Total:C}"
                };
                await smtp.SendMailAsync(mail);
                status.Status = "Sent";
                status.SentAt = DateTime.UtcNow;
                status.ErrorMessage = null;
            }
            catch (Exception ex)
            {
                status.Status = "Failed";
                status.ErrorMessage = ex.Message;
                status.RetryCount++;
                status.LastTriedAt = DateTime.UtcNow;
                if (status.RetryCount < 5)
                {
                    // Optionally, schedule a retry (could use a background job or just let the worker retry on next poll)
                }
            }
            await _db.SaveChangesAsync();
        }
    }
} 