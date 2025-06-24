using Microsoft.AspNetCore.Mvc;
using PaymentsService.Application.Services;

namespace PaymentsService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountsController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        /// <summary>
        /// Создать счёт для пользователя (если не существует).
        /// </summary>
        [HttpPost("createAccount")]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest req)
        {
            await _accountService.CreateAccountAsync(req.UserId);
            return Ok(new { Message = "Account created or already exists", AccountUserId = req.UserId });
        }

        /// <summary>
        /// Получить баланс по userId.
        /// </summary>
        [HttpGet("{userId:guid}/balance")]
        public async Task<IActionResult> GetBalance(Guid userId)
        {
            var balance = await _accountService.GetBalanceAsync(userId);
            return Ok(new BalanceResponse(balance));
        }

        /// <summary>
        /// Пополнить баланс счёта пользователя.
        /// </summary>
        [HttpPost("{userId:guid}/topup")]
        public async Task<IActionResult> TopUp(Guid userId, [FromBody] TopUpRequest req)
        {
            try
            {
                await _accountService.TopUpAsync(userId, req.Amount);
                var balance = await _accountService.GetBalanceAsync(userId);
                return Ok(new BalanceResponse(balance));
            }
            catch (InvalidOperationException)
            {
                return NotFound(new { Message = "Account not found" });
            }
        }
    }
}