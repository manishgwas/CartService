using System.Threading.Tasks;
using AuthenticationDemo.Models;

namespace AuthenticationDemo.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken> GetByTokenAsync(string token);
        Task AddAsync(RefreshToken refreshToken);
        void Revoke(RefreshToken refreshToken);
        Task SaveChangesAsync();
    }
} 