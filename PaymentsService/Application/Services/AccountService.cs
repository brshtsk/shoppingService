using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Application.Services;
using PaymentsService.Domain.Entities;
using PaymentsService.Persistence;

namespace PaymentsService.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly PaymentsDbContext _dbContext;

        public AccountService(PaymentsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task CreateAccountAsync(Guid userId)
        {
            var exists = await _dbContext.Accounts.AnyAsync(a => a.UserId == userId);
            if (exists) return;

            var account = new Account(userId);
            _dbContext.Accounts.Add(account);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<decimal> GetBalanceAsync(Guid userId)
        {
            var account = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
            return account?.Balance ?? 0m;
        }

        public async Task TopUpAsync(Guid userId, decimal amount)
        {
            var account = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
            if (account == null)
            {
                throw new InvalidOperationException("Account not found.");
            }

            // Используем доменный метод
            account.Credit(amount);

            // Пример: здесь можно также добавить запись в Outbox, если требуется
            // var payload = JsonSerializer.Serialize(new { AccountId = account.Id, Amount = amount });
            // _dbContext.OutboxMessages.Add(new OutboxMessage("AccountTopUp", payload));

            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> TryWithdrawAsync(Guid userId, decimal amount)
        {
            var account = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
            if (account == null)
            {
                return false;
            }

            // Используем доменный метод
            var success = account.TryDebit(amount);
            if (!success)
            {
                return false;
            }

            // Пример: здесь можно добавить запись в Outbox, если требуется
            // var payload = JsonSerializer.Serialize(new { AccountId = account.Id, Amount = amount });
            // _dbContext.OutboxMessages.Add(new OutboxMessage("WithdrawAttempt", payload));

            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
