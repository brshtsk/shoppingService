using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Contracts.Messaging.Configuration;
using Contracts.Messaging.Events;
using OrdersService.Persistence;
using OrdersService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace OrdersService.Application.Messaging
{
    public class PaymentCompletedConsumerService : BackgroundService
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<PaymentCompletedConsumerService> _logger;
        private readonly RabbitMqSettings _rmqSettings;

        public PaymentCompletedConsumerService(IServiceProvider provider, ILogger<PaymentCompletedConsumerService> logger, RabbitMqSettings rmqSettings)
        {
            _provider = provider;
            _logger = logger;
            _rmqSettings = rmqSettings;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _rmqSettings.Host,
                UserName = _rmqSettings.Username,
                Password = _rmqSettings.Password,
                DispatchConsumersAsync = true
            };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            channel.QueueDeclare(queue: _rmqSettings.PaymentCompletedQueue,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (sender, ea) =>
            {
                var body = ea.Body.ToArray();
                try
                {
                    var json = Encoding.UTF8.GetString(body, 0, body.Length);
                    var evt = JsonSerializer.Deserialize<PaymentCompletedEvent>(json);
                    if (evt == null)
                        throw new Exception("Cannot deserialize PaymentCompletedEvent");

                    using var scope = _provider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

                    // дедупликация
                    bool already = await db.InboxMessages.AnyAsync(im => im.Id == evt.EventId);
                    if (already)
                    {
                        _logger.LogInformation("PaymentCompletedEvent {EventId} already processed", evt.EventId);
                        channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    // обновляем заказ
                    var order = await db.Orders.FindAsync(evt.OrderId);
                    if (order == null)
                    {
                        _logger.LogWarning("Order {OrderId} not found for PaymentCompleted", evt.OrderId);
                    }
                    else
                    {
                        if (evt.Success) order.MarkPaid();
                        else order.MarkFailed();
                        _logger.LogInformation("Order {OrderId} status updated to {Status}", evt.OrderId, order.Status);
                    }

                    // записываем в Inbox
                    db.InboxMessages.Add(new InboxMessage(evt.EventId, nameof(PaymentCompletedEvent)));
                    await db.SaveChangesAsync();

                    channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing PaymentCompletedEvent");
                    channel.BasicAck(ea.DeliveryTag, false);
                }
            };

            channel.BasicConsume(queue: _rmqSettings.PaymentCompletedQueue,
                                 autoAck: false,
                                 consumer: consumer);

            return Task.CompletedTask;
        }
    }
}
