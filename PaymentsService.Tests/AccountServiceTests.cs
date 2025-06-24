using Microsoft.EntityFrameworkCore;
using PaymentsService.Application.Services;
using PaymentsService.Domain.Entities;
using PaymentsService.Persistence;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PaymentsService.Tests
{
    public class AccountServiceTests
    {
        private readonly DbContextOptions<PaymentsDbContext> _options;

        public AccountServiceTests()
        {
            _options = new DbContextOptionsBuilder<PaymentsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task GetBalance_ShouldReturnCorrectBalance()
        {
            // Arrange
            Guid userId = Guid.NewGuid();

            using (var context = new PaymentsDbContext(_options))
            {
                // Добавляем обязательное свойство Name
                var user = new User { Id = userId, Name = "TestUser" };
                context.Users.Add(user);

                var account = new Account(userId);
                account.Credit(100.0m);
                context.Accounts.Add(account);

                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new PaymentsDbContext(_options))
            {
                var accountService = new AccountService(context);
                var balance = await accountService.GetBalanceAsync(userId);

                // Assert
                Assert.Equal(100.0m, balance);
            }
        }

        [Fact]
        public async Task TopUp_ShouldIncreaseBalance()
        {
            // Arrange
            Guid userId = Guid.NewGuid();

            using (var context = new PaymentsDbContext(_options))
            {
                var user = new User { Id = userId, Name = "TestUser" };
                context.Users.Add(user);

                var account = new Account(userId);
                account.Credit(50.0m);
                context.Accounts.Add(account);

                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new PaymentsDbContext(_options))
            {
                var accountService = new AccountService(context);
                await accountService.TopUpAsync(userId, 25.0m);
            }

            // Assert
            using (var context = new PaymentsDbContext(_options))
            {
                var account = await context.Accounts.FirstAsync(a => a.UserId == userId);
                Assert.Equal(75.0m, account.Balance);
            }
        }
    }
}