using System;
using System.Collections.Generic;

namespace AuthenticationDemo.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal Total { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; }
    }
} 