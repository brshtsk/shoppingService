using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrdersService.Api.Controllers; // для OrderDto или можно в Domain мапить

namespace OrdersService.Application.Services
{
    public interface IOrderService
    {
        Task<Guid> CreateOrderAsync(Guid userId, decimal amount);
        Task<IEnumerable<OrderDto>> GetOrdersAsync(Guid userId);
        Task<string?> GetOrderStatusAsync(Guid orderId);
    }
}