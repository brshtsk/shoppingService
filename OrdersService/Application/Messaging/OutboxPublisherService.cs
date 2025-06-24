using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Microsoft.EntityFrameworkCore;
using Contracts.Messaging.Configuration;
using OrdersService.Persistence;
using OrdersService.Domain.Entities;

namespace OrdersService.Application.Messaging
{
    public class OutboxPublisherService : BackgroundService
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<OutboxPublisherService> _logger;
        private readonly RabbitMqSettings _rmqSettings;

        public OutboxPublisherService(IServiceProvider provider, ILogger<OutboxPublisherService> logger, RabbitMqSettings rmqSettings)
        {
            _provider = provider;
            _logger = logger;
            _rmqSettings = rmqSettings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OutboxPublisherService started");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _provider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

                    var batch = await db.OutboxMessages
                        .Where(o => !o.Published)
                        .OrderBy(o => o.OccurredAt)
                        .Take(20)
                        .ToListAsync(stoppingToken);

                    if (batch.Count > 0)
                    {
                        var factory = new ConnectionFactory
                        {
                            HostName = _rmqSettings.Host,
                            UserName = _rmqSettings.Username,
                            Password = _rmqSettings.Password
                        };
                        using var connection = factory.CreateConnection();
                        using var channel = connection.CreateModel();

                        foreach (var msg in batch)
                        {
                            string? queueName = msg.EventType switch
                            {
                                nameof(Contracts.Messaging.Events.OrderCreatedEvent) or "OrderCreatedEvent" 
                                    => _rmqSettings.OrderCreatedQueue,
                                _ => null
                            };

                            if (queueName != null)
                            {
                                channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);
                                var body = Encoding.UTF8.GetBytes(msg.Payload);
                                channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null, body: body);
                                msg.MarkAsPublished();
                                _logger.LogInformation("Published OrderCreatedEvent Id={Id}", msg.Id);
                            }
                            else
                            {
                                _logger.LogWarning("Unknown EventType in Outbox: {EventType}", msg.EventType);
                            }
                        }
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OutboxPublisherService");
                }
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            _logger.LogInformation("OutboxPublisherService stopping");
        }
    }
}
