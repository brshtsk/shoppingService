namespace Contracts.Messaging.Extensions;
using Contracts.Messaging.Configuration;
using RabbitMQ.Client;

public static class RabbitMqExtensions
{
    public static IConnection CreateConnection(this RabbitMqSettings settings)
    {
        var factory = new ConnectionFactory
        {
            HostName = settings.Host,
            UserName = settings.Username,
            Password = settings.Password,
            DispatchConsumersAsync = true // Включаем асинхронную обработку
        };
        return factory.CreateConnection();
    }
}