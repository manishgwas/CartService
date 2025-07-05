using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using AuthenticationDemo.Models;

namespace AuthenticationDemo.Services
{
    public class KafkaProducerService
    {
        private readonly string _bootstrapServers;
        private readonly string _topic;
        public KafkaProducerService(IConfiguration config)
        {
            _bootstrapServers = config["Kafka:BootstrapServers"];
            _topic = config["Kafka:OrderPlacedTopic"];
        }

        public async Task PublishOrderPlacedAsync(Order order)
        {
            var config = new ProducerConfig { BootstrapServers = _bootstrapServers };
            using var producer = new ProducerBuilder<Null, string>(config).Build();
            var message = new
            {
                OrderId = order.Id,
                UserId = order.UserId,
                Items = order.OrderItems,
                Total = order.Total,
                Timestamp = order.CreatedAt
            };
            var json = JsonConvert.SerializeObject(message);
            await producer.ProduceAsync(_topic, new Message<Null, string> { Value = json });
        }
    }
} 