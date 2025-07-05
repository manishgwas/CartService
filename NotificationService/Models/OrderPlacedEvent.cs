using System;
using System.Collections.Generic;

namespace NotificationService.Models
{
    public class OrderPlacedEvent
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public List<OrderItemDto> Items { get; set; }
        public decimal Total { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
} 