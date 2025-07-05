using AuthenticationDemo.DTOs;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace AuthenticationDemo.Services
{
    public class CartService : ICartService
    {
        private readonly IDatabase _redis;
        private readonly TimeSpan _cartTtl = TimeSpan.FromMinutes(30);
        public CartService(IConfiguration config)
        {
            var redis = ConnectionMultiplexer.Connect(config["Redis:ConnectionString"]);
            _redis = redis.GetDatabase();
        }

        private string GetUserCartKey(int userId) => $"cart:user:{userId}";
        private string GetGuestCartKey(string sessionKey) => $"cart:guest:{sessionKey}";

        public async Task AddItemAsync(int userId, CartItemDto item)
        {
            var cart = await GetCartAsync(userId) ?? new List<CartItemDto>();
            var existing = cart.Find(i => i.ProductId == item.ProductId);
            if (existing != null)
                existing.Quantity += item.Quantity;
            else
                cart.Add(item);
            await _redis.StringSetAsync(GetUserCartKey(userId), JsonConvert.SerializeObject(cart), _cartTtl);
        }

        public async Task RemoveItemAsync(int userId, int productId)
        {
            var cart = await GetCartAsync(userId) ?? new List<CartItemDto>();
            cart.RemoveAll(i => i.ProductId == productId);
            await _redis.StringSetAsync(GetUserCartKey(userId), JsonConvert.SerializeObject(cart), _cartTtl);
        }

        public async Task UpdateItemAsync(int userId, CartItemDto item)
        {
            var cart = await GetCartAsync(userId) ?? new List<CartItemDto>();
            var existing = cart.Find(i => i.ProductId == item.ProductId);
            if (existing != null)
                existing.Quantity = item.Quantity;
            else
                cart.Add(item);
            await _redis.StringSetAsync(GetUserCartKey(userId), JsonConvert.SerializeObject(cart), _cartTtl);
        }

        public async Task<List<CartItemDto>> GetCartAsync(int userId)
        {
            var data = await _redis.StringGetAsync(GetUserCartKey(userId));
            if (data.IsNullOrEmpty) return new List<CartItemDto>();
            return JsonConvert.DeserializeObject<List<CartItemDto>>(data);
        }

        public async Task MergeGuestCartAsync(int userId, string guestSessionKey)
        {
            var guestCart = await GetGuestCartAsync(guestSessionKey) ?? new List<CartItemDto>();
            var userCart = await GetCartAsync(userId) ?? new List<CartItemDto>();
            foreach (var item in guestCart)
            {
                var existing = userCart.Find(i => i.ProductId == item.ProductId);
                if (existing != null)
                    existing.Quantity += item.Quantity;
                else
                    userCart.Add(item);
            }
            await _redis.StringSetAsync(GetUserCartKey(userId), JsonConvert.SerializeObject(userCart), _cartTtl);
            await _redis.KeyDeleteAsync(GetGuestCartKey(guestSessionKey));
        }

        public async Task<List<CartItemDto>> GetGuestCartAsync(string sessionKey)
        {
            var data = await _redis.StringGetAsync(GetGuestCartKey(sessionKey));
            if (data.IsNullOrEmpty) return new List<CartItemDto>();
            return JsonConvert.DeserializeObject<List<CartItemDto>>(data);
        }

        public async Task AddItemToGuestCartAsync(string sessionKey, CartItemDto item)
        {
            var cart = await GetGuestCartAsync(sessionKey) ?? new List<CartItemDto>();
            var existing = cart.Find(i => i.ProductId == item.ProductId);
            if (existing != null)
                existing.Quantity += item.Quantity;
            else
                cart.Add(item);
            await _redis.StringSetAsync(GetGuestCartKey(sessionKey), JsonConvert.SerializeObject(cart), _cartTtl);
        }

        public async Task RemoveItemFromGuestCartAsync(string sessionKey, int productId)
        {
            var cart = await GetGuestCartAsync(sessionKey) ?? new List<CartItemDto>();
            cart.RemoveAll(i => i.ProductId == productId);
            await _redis.StringSetAsync(GetGuestCartKey(sessionKey), JsonConvert.SerializeObject(cart), _cartTtl);
        }

        public async Task UpdateItemInGuestCartAsync(string sessionKey, CartItemDto item)
        {
            var cart = await GetGuestCartAsync(sessionKey) ?? new List<CartItemDto>();
            var existing = cart.Find(i => i.ProductId == item.ProductId);
            if (existing != null)
                existing.Quantity = item.Quantity;
            else
                cart.Add(item);
            await _redis.StringSetAsync(GetGuestCartKey(sessionKey), JsonConvert.SerializeObject(cart), _cartTtl);
        }
    }
} 