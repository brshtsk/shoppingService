using Microsoft.EntityFrameworkCore;
using PaymentsService.Domain.Entities;

namespace PaymentsService.Persistence
{
    public class PaymentsDbContext : DbContext
    {
        public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options) { }

        public DbSet<Account> Accounts { get; set; } = null!;
        public DbSet<InboxMessage> InboxMessages { get; set; } = null!;
        public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Индекс по UserId в Account, чтобы быстро искать счёт
            modelBuilder.Entity<Account>()
                .HasIndex(a => a.UserId)
                .IsUnique();

            modelBuilder.Entity<InboxMessage>()
                .HasKey(im => im.Id);

            modelBuilder.Entity<OutboxMessage>()
                .HasKey(om => om.Id);

            // По желанию: индекс на OutboxMessage.Published для быстрого поиска непубликованных
            modelBuilder.Entity<OutboxMessage>()
                .HasIndex(o => o.Published);
        }
    }
}