namespace NjuCsCmsHelper.Server.Controllers;

using Microsoft.AspNetCore.Authentication.Cookies;

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
            new Claim(AppUserClaims.studentName, studentName),
        };
        if (tokenInfo.IsAdmin) claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(principal, new AuthenticationProperties { IsPersistent = true });

        return Ok(new { Id = studentId, Name = studentName, tokenInfo.IsAdmin });
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Ok();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> LoginJwt()
    {
        var res = await HttpContext.AuthenticateAsync("jwt");
        if (!res.Succeeded || res.Principal == null)
            return Unauthorized();
        HttpContext.User = res.Principal;

        var studentId = res.Principal.Claims.SingleOrDefault(x => x.Type == AppUserClaims.StudentId);
        if (studentId is null)
            return BadRequest("Jwt contains no studentId");

        var studentName = res.Principal.Claims.SingleOrDefault(x => x.Type == AppUserClaims.studentName);
        if (studentName is null)
            return BadRequest("Jwt contains no studentName");

        var claims = new List<Claim> {
            new Claim(AppUserClaims.StudentId, studentId.Value.ToString()),
            new Claim(AppUserClaims.studentName, studentName.Value),
        };

        var isAdmin = res.Principal.IsInRole("Admin");
        if (isAdmin) claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(principal, new AuthenticationProperties { IsPersistent = true });

        return Ok(new { Id = studentId.Value, Name = studentName.Value, IsAdmin = isAdmin });
    }

    [HttpGet]
    public IActionResult Claims()
    {
        return Ok(User.Claims);
    }
}
