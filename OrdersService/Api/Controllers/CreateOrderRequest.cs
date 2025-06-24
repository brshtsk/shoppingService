namespace OrdersService.Api.Controllers
{
    public record CreateOrderRequest(Guid UserId, decimal Amount);
}