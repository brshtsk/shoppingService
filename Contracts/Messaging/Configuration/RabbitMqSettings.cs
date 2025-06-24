namespace Contracts.Messaging.Configuration;

public class RabbitMqSettings
{
    public string Host { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string OrderCreatedQueue { get; set; } = "order_created";
    public string PaymentCompletedQueue { get; set; } = "payment_completed";
}