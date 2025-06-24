namespace PaymentsService.Api.Controllers
{
    public record CreateAccountRequest(Guid UserId);
    public record TopUpRequest(decimal Amount);
    public record BalanceResponse(decimal Balance);
}