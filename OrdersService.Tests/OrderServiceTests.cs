using Microsoft.EntityFrameworkCore;
using OrdersService.Application.Services;
using OrdersService.Domain.Entities;
using OrdersService.Persistence;
using OrdersService.Api.Controllers;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OrdersService.Tests
{
    public class OrderServiceTests
    {
        private readonly DbContextOptions<OrdersDbContext> _options;

        public OrderServiceTests()
        {
            _options = new DbContextOptionsBuilder<OrdersDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task CreateOrderAsync_ShouldCreateOrderAndOutboxMessage()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            decimal amount = 100.0m;

            // Act
            using (var context = new OrdersDbContext(_options))
            {
                var orderService = new OrderService(context);
                var orderId = await orderService.CreateOrderAsync(userId, amount);

                // Assert
                var order = await context.Orders.FindAsync(orderId);
                Assert.NotNull(order);
                Assert.Equal(userId, order.UserId);
                Assert.Equal(amount, order.Amount);
                Assert.Equal("Pending", order.Status);

                // Проверяем, что создано сообщение в Outbox
                var outboxMessage = await context.OutboxMessages
                    .FirstOrDefaultAsync(m => m.EventType == "OrderCreatedEvent");
                Assert.NotNull(outboxMessage);
            }
        }

        [Fact]
        public async Task GetOrdersAsync_ShouldReturnUserOrders()
        {
            // Arrange
            Guid userId1 = Guid.NewGuid();
            Guid userId2 = Guid.NewGuid();

            using (var context = new OrdersDbContext(_options))
            {
                // Заказы первого пользователя
                context.Orders.Add(new Order(userId1, 100.0m));
                context.Orders.Add(new Order(userId1, 150.0m));

                // Заказ второго пользователя
                context.Orders.Add(new Order(userId2, 200.0m));

                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new OrdersDbContext(_options))
            {
                var orderService = new OrderService(context);
                var orders = await orderService.GetOrdersAsync(userId1);

                // Assert
                Assert.Equal(2, orders.Count());
                Assert.All(orders, order => Assert.Equal(userId1, order.UserId));
            }
        }

        [Fact]
        public async Task GetOrderStatusAsync_ShouldReturnCorrectStatus()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Order order;

            using (var context = new OrdersDbContext(_options))
            {
                order = new Order(userId, 100.0m);
                context.Orders.Add(order);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new OrdersDbContext(_options))
            {
                var orderService = new OrderService(context);
                var status = await orderService.GetOrderStatusAsync(order.Id);

                // Assert
                Assert.Equal("Pending", status);
            }
        }
    }
}