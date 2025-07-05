using System.Threading.Tasks;
using AuthenticationDemo.Models;

namespace AuthenticationDemo.Repositories
{
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(int id);
        Task<User> GetByEmailAsync(string email);
        Task<User> GetByGoogleIdAsync(string googleId);
        Task AddAsync(User user);
        void Update(User user);
        Task SaveChangesAsync();
    }
} 