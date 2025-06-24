using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentsService.Application.Messaging;
using PaymentsService.Domain.Entities;
using PaymentsService.Persistence;
using Contracts.Messaging.Events;
using System;
using System.Threading.Tasks;
using Xunit;

namespace PaymentsService.Tests
{
    public class PaymentProcessorTests
    {
        private readonly DbContextOptions<PaymentsDbContext> _options;
        private readonly Mock<ILogger<PaymentProcessor>> _loggerMock;

        public PaymentProcessorTests()
        {
            _options = new DbContextOptionsBuilder<PaymentsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _loggerMock = new Mock<ILogger<PaymentProcessor>>();
        }

        [Fact]
        public async Task ProcessOrderCreatedAsync_WithSufficientFunds_ShouldDebitAccount()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid orderId = Guid.NewGuid();

            using (var context = new PaymentsDbContext(_options))
            {
                var user = new User { Id = userId, Name = "TestUser" };
                context.Users.Add(user);

                var account = new Account(userId);
                account.Credit(100.0m);
                context.Accounts.Add(account);

                await context.SaveChangesAsync();
            }

            var orderCreatedEvent = new OrderCreatedEvent(
                Guid.NewGuid(),
                orderId,
                userId,
                50.0m);

            // Act
            using (var context = new PaymentsDbContext(_options))
            {
                var processor = new PaymentProcessor(context, _loggerMock.Object);
                var result = await processor.ProcessOrderCreatedAsync(orderCreatedEvent);

                // Assert
                Assert.True(result.Success);
            }

            // Проверка баланса
            using (var context = new PaymentsDbContext(_options))
            {
                var account = await context.Accounts.FirstAsync(a => a.UserId == userId);
                Assert.Equal(50.0m, account.Balance);
            }
        }

        [Fact]
        public async Task ProcessOrderCreatedAsync_WithInsufficientFunds_ShouldFail()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid orderId = Guid.NewGuid();

            using (var context = new PaymentsDbContext(_options))
            {
                var user = new User { Id = userId, Name = "TestUser" };
                context.Users.Add(user);

                var account = new Account(userId);
                account.Credit(20.0m);
                context.Accounts.Add(account);

                await context.SaveChangesAsync();
            }

            var orderCreatedEvent = new OrderCreatedEvent(
                Guid.NewGuid(),
                orderId,
                userId,
                50.0m); // больше чем баланс

            // Act
            using (var context = new PaymentsDbContext(_options))
            {
                var processor = new PaymentProcessor(context, _loggerMock.Object);
                var result = await processor.ProcessOrderCreatedAsync(orderCreatedEvent);

                // Assert
                Assert.False(result.Success);
            }

            // Проверка что баланс не изменился
            using (var context = new PaymentsDbContext(_options))
            {
                var account = await context.Accounts.FirstAsync(a => a.UserId == userId);
                Assert.Equal(20.0m, account.Balance);
            }
        }
    }
}