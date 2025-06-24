using System;
using System.Threading.Tasks;

namespace PaymentsService.Application.Services
{
    public interface IAccountService
    {
        Task CreateAccountAsync(Guid userId);
        Task<decimal> GetBalanceAsync(Guid userId);
        Task TopUpAsync(Guid userId, decimal amount);
        Task<bool> TryWithdrawAsync(Guid userId, decimal amount);
    }
}