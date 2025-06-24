using System;

namespace OrdersService.Api.Controllers
{
    public record OrderStatusResponse(Guid OrderId, string Status);
}