using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrdersService.Domain.Entities;
using OrdersService.Persistence;
using Contracts.Messaging.Events;
using OrdersService.Api.Controllers;

namespace OrdersService.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly OrdersDbContext _db;

        public OrderService(OrdersDbContext db)
        {
            _db = db;
        }

        public async Task<Guid> CreateOrderAsync(Guid userId, decimal amount)
        {
            // создаём заказ в статусе Pending, записываем в БД и в Outbox событие
            var order = new Order(userId, amount);
            _db.Orders.Add(order);

            // формируем событие OrderCreatedEvent из Contracts
            var evt = new OrderCreatedEvent(
                Guid.NewGuid(),
                order.Id,
                userId,
                amount
            );
            var payload = JsonSerializer.Serialize(evt);
            _db.OutboxMessages.Add(new OrdersService.Domain.Entities.OutboxMessage(nameof(OrderCreatedEvent), payload));

            await _db.SaveChangesAsync();
            return order.Id;
        }

        public async Task<IEnumerable<OrderDto>> GetOrdersAsync(Guid userId)
        {
            return await _db.Orders
                .Where(o => o.UserId == userId)
                .Select(o => new OrderDto(o.Id, o.UserId, o.Amount, o.Status, o.CreatedAt, o.UpdatedAt))
                .ToListAsync();
        }

        public async Task<string?> GetOrderStatusAsync(Guid orderId)
        {
            var order = await _db.Orders.FindAsync(orderId);
            return order?.Status;
        }
    }
}