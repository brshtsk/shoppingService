using System;
using System.Threading.Tasks;
using PaymentsService.Domain.Entities;
using PaymentsService.Persistence;

namespace PaymentsService.Application.Services
{
    public class UserService : IUserService
    {
        private readonly PaymentsDbContext _db;
        public UserService(PaymentsDbContext db) => _db = db;

        public async Task<Guid> CreateUserAsync(string name)
        {
            var user = new User { Id = Guid.NewGuid(), Name = name };
            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();
            return user.Id;
        }
    }
}