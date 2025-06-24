using System;

namespace OrdersService.Domain.Entities
{
    public class InboxMessage
    {
        public Guid Id { get; private set; } // EventId из PaymentCompletedEvent
        public string EventType { get; private set; } = null!;
        public DateTime ProcessedAt { get; private set; }

        private InboxMessage()
        {
        }

        public InboxMessage(Guid eventId, string eventType)
        {
            Id = eventId;
            EventType = eventType;
            ProcessedAt = DateTime.UtcNow;
        }
    }
}