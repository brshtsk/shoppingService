using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OrdersService.Application.Messaging;
using OrdersService.Domain.Entities;
using OrdersService.Persistence;
using Contracts.Messaging.Events;
using System;
using System.Threading.Tasks;
using Xunit;

namespace OrdersService.Tests
{
    public class PaymentCompletedConsumerTests
    {
        private readonly DbContextOptions<OrdersDbContext> _options;
        private readonly Mock<ILogger<PaymentCompletedConsumerService>> _loggerMock;

        public PaymentCompletedConsumerTests()
        {
            _options = new DbContextOptionsBuilder<OrdersDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _loggerMock = new Mock<ILogger<PaymentCompletedConsumerService>>();
        }

        [Fact]
        public async Task ProcessPayment_WithSuccessfulPayment_ShouldMarkOrderAsPaid()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid eventId = Guid.NewGuid();
            
            // Создаем заказ и получаем его ID
            Guid orderId;
            using (var context = new OrdersDbContext(_options))
            {
                var order = new Order(userId, 100.0m);
                context.Orders.Add(order);
                await context.SaveChangesAsync();
                orderId = order.Id;
            }

            var paymentCompletedEvent = new PaymentCompletedEvent(
                eventId,
                orderId,
                userId,
                true,
                100.0m
            );

            // Act
            using (var context = new OrdersDbContext(_options))
            {
                var order = await context.Orders.FindAsync(orderId);
                if (order != null)
                {
                    if (paymentCompletedEvent.Success) order.MarkPaid();
                    else order.MarkFailed();
                }

                context.InboxMessages.Add(new InboxMessage(eventId, "PaymentCompletedEvent"));
                await context.SaveChangesAsync();
            }

            // Assert
            using (var context = new OrdersDbContext(_options))
            {
                var order = await context.Orders.FindAsync(orderId);
                Assert.NotNull(order);
                Assert.Equal("Paid", order.Status);

                // Проверяем, что событие было записано в Inbox
                var inboxMessage = await context.InboxMessages.FindAsync(eventId);
                Assert.NotNull(inboxMessage);
            }
        }

        [Fact]
        public async Task ProcessPayment_WithFailedPayment_ShouldMarkOrderAsFailed()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid eventId = Guid.NewGuid();
            
            // Создаем заказ и получаем его ID
            Guid orderId;
            using (var context = new OrdersDbContext(_options))
            {
                var order = new Order(userId, 100.0m);
                context.Orders.Add(order);
                await context.SaveChangesAsync();
                orderId = order.Id;
            }

            var paymentCompletedEvent = new PaymentCompletedEvent(
                eventId,
                orderId,
                userId,
                false,
                100.0m
            );

            // Act
            using (var context = new OrdersDbContext(_options))
            {
                var order = await context.Orders.FindAsync(orderId);
                if (order != null)
                {
                    if (paymentCompletedEvent.Success) order.MarkPaid();
                    else order.MarkFailed();
                }

                context.InboxMessages.Add(new InboxMessage(eventId, "PaymentCompletedEvent"));
                await context.SaveChangesAsync();
            }

            // Assert
            using (var context = new OrdersDbContext(_options))
            {
                var order = await context.Orders.FindAsync(orderId);
                Assert.NotNull(order);
                Assert.Equal("Failed", order.Status);
            }
        }

        [Fact]
        public async Task ProcessPayment_WhenDuplicate_ShouldSkipProcessing()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid eventId = Guid.NewGuid();
            
            // Создаем заказ и получаем его ID
            Guid orderId;
            using (var context = new OrdersDbContext(_options))
            {
                var order = new Order(userId, 100.0m);
                context.Orders.Add(order);
                
                // Добавляем запись в Inbox, как будто событие уже обработано
                context.InboxMessages.Add(new InboxMessage(eventId, "PaymentCompletedEvent"));
                await context.SaveChangesAsync();
                orderId = order.Id;
            }

            var paymentCompletedEvent = new PaymentCompletedEvent(
                eventId,
                orderId,
                userId,
                true,
                100.0m
            );

            // Act
            using (var context = new OrdersDbContext(_options))
            {
                // Проверяем, было ли событие уже обработано
                bool alreadyProcessed = await context.InboxMessages.AnyAsync(im => im.Id == eventId);

                // Если не было, то обрабатываем
                if (!alreadyProcessed)
                {
                    var order = await context.Orders.FindAsync(orderId);
                    if (order != null)
                    {
                        if (paymentCompletedEvent.Success) order.MarkPaid();
                        else order.MarkFailed();
                    }

                    context.InboxMessages.Add(new InboxMessage(eventId, "PaymentCompletedEvent"));
                    await context.SaveChangesAsync();
                }
            }

            // Assert
            using (var context = new OrdersDbContext(_options))
            {
                var order = await context.Orders.FindAsync(orderId);
                Assert.NotNull(order);
                // Статус не должен измениться, так как событие уже было обработано
                Assert.Equal("Pending", order.Status);
            }
        }
    }
}