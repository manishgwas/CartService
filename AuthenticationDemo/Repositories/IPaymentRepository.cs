using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthenticationDemo.Models;

namespace AuthenticationDemo.Repositories
{
    public interface IPaymentRepository
    {
        Task<Payment> CreateAsync(Payment payment);
        Task<Payment> GetByIdAsync(int id);
        Task<Payment> GetByRazorpayPaymentIdAsync(string razorpayPaymentId);
        Task<Payment> GetByRazorpayOrderIdAsync(string razorpayOrderId);
        Task<IEnumerable<Payment>> GetByUserIdAsync(string userId);
        Task<IEnumerable<Payment>> GetByOrderIdAsync(string orderId);
        Task<Payment> UpdateAsync(Payment payment);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<Payment>> GetAllAsync();
        Task<IEnumerable<Payment>> GetByStatusAsync(string status);
    }
} 