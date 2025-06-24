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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OrderCreatedConsumerService инициализация...");

            // Асинхронная задержка перед стартом - 5 секунд достаточно
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            _logger.LogInformation("OrderCreatedConsumerService подключение к RabbitMQ...");

            var factory = new ConnectionFactory
            {
                HostName = _rmqSettings.Host,
                UserName = _rmqSettings.Username,
                Password = _rmqSettings.Password,
                DispatchConsumersAsync = true
            };

            // Обработка подключения с повторными попытками
            IConnection connection = null;
            int retryCount = 0;

            while (connection == null && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    connection = factory.CreateConnection();
                }
                catch (Exception ex)
                {
                    retryCount++;
                    var delay = TimeSpan.FromSeconds(Math.Min(3 * retryCount, 15));
                    _logger.LogWarning(ex, "Не удалось подключиться к RabbitMQ, повторная попытка через {delay} сек.",
                        delay.TotalSeconds);
                    await Task.Delay(delay, stoppingToken);
                }
            }

            if (connection == null || stoppingToken.IsCancellationRequested)
                return;

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
                    // Реализуем Exactly Once: если была ошибка при снятии денег, кидаем запрос снова в очередь
                    channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            channel.BasicConsume(
                queue: _rmqSettings.OrderCreatedQueue,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("OrderCreatedConsumerService начал прослушивание очереди {queue}",
                _rmqSettings.OrderCreatedQueue);

            // Ожидаем отмены задачи
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}