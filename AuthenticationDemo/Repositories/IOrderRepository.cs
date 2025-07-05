using System.Collections.Generic;
using System.Threading.Tasks;
using AuthenticationDemo.Models;

namespace AuthenticationDemo.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> GetByIdAsync(int id);
        Task<List<Order>> GetByUserIdAsync(int userId);
        Task AddAsync(Order order);
        Task SaveChangesAsync();
    }
} 