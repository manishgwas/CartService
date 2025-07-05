using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        var config = hostContext.Configuration;
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseSqlite(config.GetConnectionString("DefaultConnection") ?? "Data Source=notification.db"));
        services.AddScoped<EmailService>();
        services.AddSingleton<KafkaConsumerService>(sp =>
        {
            var emailService = sp.GetRequiredService<EmailService>();
            return new KafkaConsumerService(
                config["Kafka:BootstrapServers"] ?? "localhost:9092",
                config["Kafka:OrderPlacedTopic"] ?? "OrderPlaced",
                emailService);
        });
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
