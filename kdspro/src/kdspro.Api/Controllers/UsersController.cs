using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using kdspro.Domain.Interfaces;

namespace kdspro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _users;

    public UsersController(IUserRepository users)
    {
        _users = users;
    }

    [HttpGet("waiters")]
    [Authorize(Roles = "host,admin")]
    public async Task<IActionResult> GetWaiters()
    {
        var waiters = await _users.GetByRole("waiter");

        return Ok(waiters.Select(user => new
        {
            id = user.Id,
            username = user.Username,
            role = user.Role,
        }));
    }
}
