using System.Collections.Generic;
using System.Threading.Tasks;
using AuthenticationDemo.DTOs;

namespace AuthenticationDemo.Services
{
    public interface ICartService
    {
        Task AddItemAsync(int userId, CartItemDto item);
        Task RemoveItemAsync(int userId, int productId);
        Task UpdateItemAsync(int userId, CartItemDto item);
        Task<List<CartItemDto>> GetCartAsync(int userId);
        Task MergeGuestCartAsync(int userId, string guestSessionKey);
        Task<List<CartItemDto>> GetGuestCartAsync(string sessionKey);
        Task AddItemToGuestCartAsync(string sessionKey, CartItemDto item);
        Task RemoveItemFromGuestCartAsync(string sessionKey, int productId);
        Task UpdateItemInGuestCartAsync(string sessionKey, CartItemDto item);
    }
} 