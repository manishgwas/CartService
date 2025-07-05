using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AuthenticationDemo.Models;
using AuthenticationDemo.Data;

namespace AuthenticationDemo.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetByIdAsync(int id) =>
            await _context.Users.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.Id == id);

        public async Task<User> GetByEmailAsync(string email) =>
            await _context.Users.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.Email == email);

        public async Task<User> GetByGoogleIdAsync(string googleId) =>
            await _context.Users.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.GoogleId == googleId);

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public void Update(User user)
        {
            _context.Users.Update(user);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
} 