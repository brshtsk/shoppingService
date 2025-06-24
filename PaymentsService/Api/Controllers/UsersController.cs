using Microsoft.AspNetCore.Mvc;
using PaymentsService.Application.Services;
using System;
using System.Threading.Tasks;

namespace PaymentsService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        public UsersController(IUserService userService) => _userService = userService;

        
        /// <summary>
        /// Создать пользователя и вернуть его userId.
        /// </summary>
        [HttpPost("newUser")]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
        {
            var userId = await _userService.CreateUserAsync(req.Name);
            return Ok(new { UserId = userId });
        }
    }
}