using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AuthenticationDemo.Models;
using AuthenticationDemo.Data;

namespace AuthenticationDemo.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ApplicationDbContext _context;
        public RefreshTokenRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken> GetByTokenAsync(string token) =>
            await _context.RefreshTokens.Include(rt => rt.User).FirstOrDefaultAsync(rt => rt.Token == token);

        public async Task AddAsync(RefreshToken refreshToken)
        {
            await _context.RefreshTokens.AddAsync(refreshToken);
        }

        public void Revoke(RefreshToken refreshToken)
        {
            refreshToken.Revoked = System.DateTime.UtcNow;
            _context.RefreshTokens.Update(refreshToken);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
} 