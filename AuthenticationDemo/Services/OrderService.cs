using AuthenticationDemo.DTOs;
using AuthenticationDemo.Models;
using AuthenticationDemo.Repositories;
using StackExchange.Redis;

namespace AuthenticationDemo.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly ICartService _cartService;
        private readonly KafkaProducerService _kafkaProducer;
        private readonly IDatabase _redis;
        private readonly TimeSpan _idempotencyTtl = TimeSpan.FromHours(24);
        public OrderService(IOrderRepository orderRepo, ICartService cartService, KafkaProducerService kafkaProducer, IConfiguration config)
        {
            _orderRepo = orderRepo;
            _cartService = cartService;
            _kafkaProducer = kafkaProducer;
            var redis = ConnectionMultiplexer.Connect(config["Redis:ConnectionString"]);
            _redis = redis.GetDatabase();
        }

        public async Task<OrderResponseDto> CheckoutAsync(int userId, OrderRequestDto dto, string idempotencyKey)
        {
            // Idempotency check
            var idempotencyRedisKey = $"idempotency:order:{userId}:{idempotencyKey}";
            var existingOrderId = await _redis.StringGetAsync(idempotencyRedisKey);
            if (!existingOrderId.IsNullOrEmpty)
            {
                // Already processed, return existing order
                var orderId = int.Parse(existingOrderId);
                var existingOrder = await _orderRepo.GetByIdAsync(orderId);
                return new OrderResponseDto
                {
                    OrderId = existingOrder.Id,
                    Items = existingOrder.OrderItems.Select(oi => new CartItemDto { ProductId = oi.ProductId, Quantity = oi.Quantity }).ToList(),
                    Total = existingOrder.Total,
                    Timestamp = existingOrder.CreatedAt
                };
            }

            // Merge cart from Redis
            var cartItems = await _cartService.GetCartAsync(userId);
            if (cartItems == null || !cartItems.Any())
                throw new Exception("Cart is empty");

            // Save order and items in DB
            var order = new AuthenticationDemo.Models.Order
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                Total = cartItems.Sum(i => i.Quantity * 10), // Replace 10 with actual price lookup if needed
                OrderItems = cartItems.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = 10 // Replace with actual price
                }).ToList()
            };
            await _orderRepo.AddAsync(order);
            await _orderRepo.SaveChangesAsync();

            // Clear Redis cart
            var cartKey = $"cart:user:{userId}";
            await _redis.KeyDeleteAsync(cartKey);

            // Store idempotency key
            await _redis.StringSetAsync(idempotencyRedisKey, order.Id, _idempotencyTtl);

            // Publish to Kafka
            await _kafkaProducer.PublishOrderPlacedAsync(order);

            return new OrderResponseDto
            {
                OrderId = order.Id,
                Items = cartItems,
                Total = order.Total,
                Timestamp = order.CreatedAt
            };
        }
    }
} 