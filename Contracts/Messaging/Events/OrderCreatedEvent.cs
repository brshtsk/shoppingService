namespace Contracts.Messaging.Events;

public class OrderCreatedEvent
{
    public Guid EventId { get; init; }
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public decimal Amount { get; init; }
    
    public OrderCreatedEvent(Guid eventId, Guid orderId, Guid userId, decimal amount)
    {
        EventId = eventId;
        OrderId = orderId;
        UserId = userId;
        Amount = amount;
    }
}