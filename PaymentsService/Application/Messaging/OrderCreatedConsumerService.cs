using System.Text;
using System.Text.Json;
using Contracts.Messaging.Events;
using PaymentsService.Persistence;
using Contracts.Messaging.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PaymentsService.Application.Messaging
{
    public class OrderCreatedConsumerService : BackgroundService
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<OrderCreatedConsumerService> _logger;
        private readonly RabbitMqSettings _rmqSettings;

        public OrderCreatedConsumerService(
            IServiceProvider provider,
            ILogger<OrderCreatedConsumerService> logger,
            RabbitMqSettings rmqSettings)
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

            channel.QueueDeclare(
                queue: _rmqSettings.OrderCreatedQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (sender, ea) =>
            {
                var body = ea.Body.ToArray();
                try
                {
                    var json = Encoding.UTF8.GetString(body, 0, body.Length);
                    var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(json);
                    if (evt == null)
                        throw new Exception("Не удалось десериализовать OrderCreatedEvent");

                    using var scope = _provider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
                    var loggerProc = scope.ServiceProvider.GetRequiredService<ILogger<PaymentProcessor>>();
                    var processor = new PaymentProcessor(db, loggerProc);

                    var result = await processor.ProcessOrderCreatedAsync(evt);
                    _logger.LogInformation("Обработан OrderCreatedEvent: OrderId={OrderId}, Success={Success}", result.OrderId, result.Success);

                    channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при обработке сообщения OrderCreated");
                    channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
            };

            channel.BasicConsume(
                queue: _rmqSettings.OrderCreatedQueue,
                autoAck: false,
                consumer: consumer);

            return Task.CompletedTask;
        }
    }
}
