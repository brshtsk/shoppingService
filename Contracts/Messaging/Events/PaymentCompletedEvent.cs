namespace Contracts.Messaging.Events;

public class PaymentCompletedEvent
{
    public Guid EventId { get; init; }
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public bool Success { get; init; }
    public decimal Amount { get; init; }
    
    public PaymentCompletedEvent(Guid eventId, Guid orderId, Guid userId, bool success, decimal amount)
    {
        EventId = eventId;
        OrderId = orderId;
        UserId = userId;
        Success = success;
        Amount = amount;
    }
}