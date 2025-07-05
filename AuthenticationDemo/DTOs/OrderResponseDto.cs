using System;
using System.Collections.Generic;

namespace AuthenticationDemo.DTOs
{
    public class OrderResponseDto
    {
        public int OrderId { get; set; }
        public List<CartItemDto> Items { get; set; }
        public decimal Total { get; set; }
        public DateTime Timestamp { get; set; }
    }
} 