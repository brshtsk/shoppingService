using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PaymentsService.Persistence
{
    public class PaymentsDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
    {
        public PaymentsDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<PaymentsDbContext>();
            // Локальная строка для миграций:
            builder.UseNpgsql("Host=localhost;Port=5432;Database=payments_db;Username=postgres;Password=postgres");
            return new PaymentsDbContext(builder.Options);
        }
    }
}