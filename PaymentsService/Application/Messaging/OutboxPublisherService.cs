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
using Contracts.Messaging.Configuration;       // RabbitMqSettings
using PaymentsService.Persistence; // PaymentsDbContext
using PaymentsService.Domain.Entities;         // OutboxMessage

namespace PaymentsService.Application.Messaging
{
    /// <summary>
    /// HostedService, который периодически сканирует таблицу OutboxMessages
    /// и публикует непубликованные события в RabbitMQ, помечая их Published=true.
    /// </summary>
    public class OutboxPublisherService : BackgroundService
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<OutboxPublisherService> _logger;
        private readonly RabbitMqSettings _rmqSettings;

        public OutboxPublisherService(
            IServiceProvider provider,
            ILogger<OutboxPublisherService> logger,
            RabbitMqSettings rmqSettings)
        {
            _provider = provider;
            _logger = logger;
            _rmqSettings = rmqSettings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // ToDo: проверить, надо ли
            // Задержка перед стартом
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            
            _logger.LogInformation("OutboxPublisherService started");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _provider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

                    // Берём батч непубликованных сообщений
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
                                nameof(Contracts.Messaging.Events.PaymentCompletedEvent) or "PaymentCompleted"
                                    => _rmqSettings.PaymentCompletedQueue,
                                // Добавьте здесь другие EventType, если нужно (например, AccountTopUp и т.п.)
                                _ => null
                            };

                            if (queueName != null)
                            {
                                // Объявляем очередь на случай, если ещё не создана
                                channel.QueueDeclare(
                                    queue: queueName,
                                    durable: true,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

                                var body = Encoding.UTF8.GetBytes(msg.Payload);
                                channel.BasicPublish(
                                    exchange: "",
                                    routingKey: queueName,
                                    basicProperties: null,
                                    body: body);

                                msg.MarkAsPublished();
                                _logger.LogInformation("Published Outbox message Id={Id}, EventType={EventType} to queue={Queue}",
                                    msg.Id, msg.EventType, queueName);
                            }
                            else
                            {
                                _logger.LogWarning("Unknown EventType in OutboxMessage: {EventType}", msg.EventType);
                            }
                        }

                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OutboxPublisherService");
                }

                // Ждём перед следующей итерацией
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            _logger.LogInformation("OutboxPublisherService stopping");
        }
    }
}
