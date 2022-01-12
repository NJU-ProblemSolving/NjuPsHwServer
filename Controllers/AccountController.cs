using System.Text.RegularExpressions;

namespace NjuCsCmsHelper.Server.Controllers;

using Models;

[Route("api/[controller]/[action]")]
[ApiController]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly ILogger<AccountController> logger;

    public AccountController(ILogger<AccountController> logger) { this.logger = logger; }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] string token)
    {
        if (token == "d2620ea616604b0ab5421cd434ceda87")
        {
            var claims = new Claim[] {
                new Claim("user", "AdminUser"),
                new Claim("role", "Admin"),
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(principal);
            return Ok();
        }
        else
        {
            return Unauthorized();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Ok();
    }
}
