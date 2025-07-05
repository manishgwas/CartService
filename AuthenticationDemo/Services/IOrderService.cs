using System.Threading.Tasks;
using AuthenticationDemo.DTOs;

namespace AuthenticationDemo.Services
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CheckoutAsync(int userId, OrderRequestDto dto, string idempotencyKey);
    }
} 