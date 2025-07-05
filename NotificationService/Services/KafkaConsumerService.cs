using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Newtonsoft.Json;
using NotificationService.Models;

namespace NotificationService.Services
{
    public class KafkaConsumerService
    {
        private readonly string _bootstrapServers;
        private readonly string _topic;
        private readonly string _groupId = "notification-service";
        private readonly EmailService _emailService;
        public KafkaConsumerService(string bootstrapServers, string topic, EmailService emailService)
        {
            _bootstrapServers = bootstrapServers;
            _topic = topic;
            _emailService = emailService;
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = _groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe(_topic);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(stoppingToken);
                    var orderEvent = JsonConvert.DeserializeObject<OrderPlacedEvent>(cr.Message.Value);
                    // TODO: Lookup user email from UserId (could be via API or DB)
                    var email = $"user{orderEvent.UserId}@demo.com"; // Placeholder
                    await _emailService.SendOrderConfirmationAsync(orderEvent, email);
                }
                catch (Exception ex)
                {
                    // Log and continue
                }
            }
        }
    }
} 