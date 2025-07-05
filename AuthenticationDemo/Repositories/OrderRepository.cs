using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AuthenticationDemo.Models;
using AuthenticationDemo.Data;

namespace AuthenticationDemo.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;
        public OrderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Order> GetByIdAsync(int id) =>
            await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id);

        public async Task<List<Order>> GetByUserIdAsync(int userId) =>
            await _context.Orders.Include(o => o.OrderItems).Where(o => o.UserId == userId).ToListAsync();

        public async Task AddAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
} 