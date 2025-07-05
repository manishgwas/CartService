using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NotificationService.Services;

namespace NotificationService;

public class Worker : BackgroundService
{
    private readonly KafkaConsumerService _kafkaConsumerService;

    public Worker(KafkaConsumerService kafkaConsumerService)
    {
        _kafkaConsumerService = kafkaConsumerService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _kafkaConsumerService.StartAsync(stoppingToken);
    }
}
