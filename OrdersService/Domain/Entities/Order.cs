using System;

namespace OrdersService.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public decimal Amount { get; private set; }
        public string Status { get; private set; } // "Pending", "Paid", "Failed"
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        private Order()
        {
        }

        public Order(Guid userId, decimal amount)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            Amount = amount;
            Status = "Pending";
            CreatedAt = DateTime.UtcNow;
        }

        public void MarkPaid()
        {
            if (Status != "Pending") return;
            Status = "Paid";
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkFailed()
        {
            if (Status != "Pending") return;
            Status = "Failed";
            UpdatedAt = DateTime.UtcNow;
        }
    }
}