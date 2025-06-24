namespace PaymentsService.Domain.Entities
{
    public class Account
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public decimal Balance { get; private set; }
        public DateTime CreatedAt { get; private set; }

        // Для EF Core
        private Account() { }

        public Account(Guid userId)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            Balance = 0m;
            CreatedAt = DateTime.UtcNow;
        }

        public void Credit(decimal amount)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));
            Balance += amount;
        }
        
        // ToDo: проверить корректность списания с баланса

        public bool TryDebit(decimal amount)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));
            if (Balance < amount) return false;
            Balance -= amount;
            return true;
        }
    }
}