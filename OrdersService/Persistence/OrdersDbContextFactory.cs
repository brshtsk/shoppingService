using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrdersService.Persistence
{
    public class OrdersDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
    {
        public OrdersDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<OrdersDbContext>();
            // Локальная строка для миграций: убедитесь, что локальный Postgres доступен на этом порту
            builder.UseNpgsql("Host=localhost;Port=5433;Database=orders_db;Username=postgres;Password=postgres");
            return new OrdersDbContext(builder.Options);
        }
    }
}