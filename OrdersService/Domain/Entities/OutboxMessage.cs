using System;

namespace OrdersService.Domain.Entities
{
    public class OutboxMessage
    {
        public Guid Id { get; private set; }
        public string EventType { get; private set; } = null!;
        public string Payload { get; private set; } = null!;
        public DateTime OccurredAt { get; private set; }
        public bool Published { get; private set; }
        public DateTime? PublishedAt { get; private set; }

        private OutboxMessage() { }
        public OutboxMessage(string eventType, string payload)
        {
            Id = Guid.NewGuid();
            EventType = eventType;
            Payload = payload;
            OccurredAt = DateTime.UtcNow;
            Published = false;
        }

        public void MarkAsPublished()
        {
            Published = true;
            PublishedAt = DateTime.UtcNow;
        }
    }
}