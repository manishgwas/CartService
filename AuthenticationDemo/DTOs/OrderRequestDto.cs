using System.Collections.Generic;

namespace AuthenticationDemo.DTOs
{
    public class OrderRequestDto
    {
        public List<CartItemDto> Items { get; set; }
        // Add more fields as needed (e.g., ShippingAddress)
    }
} 