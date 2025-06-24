using Microsoft.EntityFrameworkCore;
using OrdersService.Domain.Entities;

namespace OrdersService.Persistence
{
    public class OrdersDbContext : DbContext
    {
        public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }

        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<InboxMessage> InboxMessages { get; set; } = null!;
        public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.UserId);
            modelBuilder.Entity<InboxMessage>()
                .HasKey(im => im.Id);
            modelBuilder.Entity<OutboxMessage>()
                .HasKey(om => om.Id);
            modelBuilder.Entity<OutboxMessage>()
                .HasIndex(o => o.Published);
        }
    }
}