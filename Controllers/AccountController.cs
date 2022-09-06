namespace NjuCsCmsHelper.Server.Controllers;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;

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
            new Claim(AppUserClaims.StudentId, studentId.ToString(NumberFormatInfo.InvariantInfo)),
            new Claim(AppUserClaims.StudentName, studentName),
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
        var res = await HttpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (!res.Succeeded || res.Principal == null)
            return Unauthorized();
        this.HttpContext.User = res.Principal;

        var studentIdClaim = res.Principal.Claims.SingleOrDefault(x => x.Type == AppUserClaims.StudentId);
        if (studentIdClaim is null)
            return BadRequest("Jwt contains no studentId");
        var studentId = int.Parse(studentIdClaim.Value);

        var studentName = res.Principal.Claims.SingleOrDefault(x => x.Type == AppUserClaims.StudentName);
        if (studentName is null)
            return BadRequest("Jwt contains no studentName");

        var token = await dbContext.Tokens.FirstOrDefaultAsync(x => x.StudentId == studentId);
        if (token is null)
            return Unauthorized("Student is not registered");

        var claims = new List<Claim> {
            new Claim(AppUserClaims.StudentId, studentId.ToString()),
            new Claim(AppUserClaims.StudentName, studentName.Value),
        };

        var isAdmin = res.Principal.IsInRole("Administrator") && token.IsAdmin;
        if (isAdmin) claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(principal, new AuthenticationProperties { IsPersistent = true });

        return Ok(new { Id = studentId, Name = studentName.Value, IsAdmin = isAdmin, Token = token.Id });
    }

    [HttpGet]
    public IActionResult Claims()
    {
        return Ok(User.Claims.Select(x => (x.Type, x.Value)));
    }
}
