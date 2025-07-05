using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AuthenticationDemo.Data;
using AuthenticationDemo.Models;

namespace AuthenticationDemo.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ApplicationDbContext _context;

        public PaymentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Payment> CreateAsync(Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<Payment> GetByIdAsync(int id)
        {
            return await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Payment> GetByRazorpayPaymentIdAsync(string razorpayPaymentId)
        {
            return await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.RazorpayPaymentId == razorpayPaymentId);
        }

        public async Task<Payment> GetByRazorpayOrderIdAsync(string razorpayOrderId)
        {
            return await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.RazorpayOrderId == razorpayOrderId);
        }

        public async Task<IEnumerable<Payment>> GetByUserIdAsync(string userId)
        {
            return await _context.Payments
                .Include(p => p.Order)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetByOrderIdAsync(string orderId)
        {
            return await _context.Payments
                .Include(p => p.User)
                .Where(p => p.OrderId == orderId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Payment> UpdateAsync(Payment payment)
        {
            payment.UpdatedAt = DateTime.UtcNow;
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return false;

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Payment>> GetAllAsync()
        {
            return await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Order)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetByStatusAsync(string status)
        {
            return await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Order)
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
    }
} 