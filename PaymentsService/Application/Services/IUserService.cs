using System;
using System.Threading.Tasks;

namespace PaymentsService.Application.Services
{
    public interface IUserService
    {
        Task<Guid> CreateUserAsync(string name);
    }
}