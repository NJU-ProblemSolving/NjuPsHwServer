namespace NjuCsCmsHelper.Server.Controllers;

using Models;

[Route("api/[controller]/[action]")]
[ApiController]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly ILogger<AccountController> logger;
    private readonly AppDbContext dbContext;

    public AccountController(ILogger<AccountController> logger, AppDbContext dbContext)
    {
        this.logger = logger;
        this.dbContext = dbContext;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] string token)
    {
        var tokenInfo = await dbContext.Tokens.Include(x => x.Student).SingleOrDefaultAsync(x => x.Id == token);
        if (tokenInfo == null) return Unauthorized();

        var studentId = tokenInfo.StudentId;
        var studentName = tokenInfo.Student.Name;
        var claims = new List<Claim> {
            new Claim(AppUserClaims.StudentId, studentId.ToString()),
            new Claim(AppUserClaims.Name, studentName),
        };
        if (tokenInfo.IsAdmin) claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(principal, new AuthenticationProperties { IsPersistent = true });

        return Ok(new { Id = studentId, Name = studentName, IsAdmin = tokenInfo.IsAdmin });
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Ok();
    }
}
