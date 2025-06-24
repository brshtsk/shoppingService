using System;

namespace OrdersService.Api.Controllers
{
    public record OrderDto(Guid Id, Guid UserId, decimal Amount, string Status, DateTime CreatedAt, DateTime? UpdatedAt);
}