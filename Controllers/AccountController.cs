namespace NjuCsCmsHelper.Server.Controllers;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;

[Route("api/[controller]/[action]")]
[ApiController]
[Authorize]
public class AccountController : AppControllerBase<AccountController>
{
    public AccountController(IServiceProvider provider) : base(provider) { }

    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AccountInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([Required, FromBody] string token)
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

        return Ok(new AccountInfo { Id = studentId, Name = studentName, IsAdmin = tokenInfo.IsAdmin });
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Ok();
    }

    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AccountInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginJwt()
    {
        var res = await HttpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (!res.Succeeded || res.Principal == null)
            return Unauthorized();
        this.HttpContext.User = res.Principal;

        var studentIdClaim = res.Principal.Claims.SingleOrDefault(x => x.Type == AppUserClaims.StudentId);
        if (studentIdClaim is null)
            return BadRequest("Jwt contains no studentId");
        var studentId = int.Parse(studentIdClaim.Value, CultureInfo.InvariantCulture);

        var studentName = res.Principal.Claims.SingleOrDefault(x => x.Type == AppUserClaims.StudentName);
        if (studentName is null)
            return BadRequest("Jwt contains no studentName");

        var token = await dbContext.Tokens.FirstOrDefaultAsync(x => x.StudentId == studentId);
        if (token is null)
            return Unauthorized("Student is not registered");

        var claims = new List<Claim> {
            new Claim(AppUserClaims.StudentId, studentId.ToString(CultureInfo.InvariantCulture)),
            new Claim(AppUserClaims.StudentName, studentName.Value),
        };

        var isAdmin = res.Principal.IsInRole("Administrator") && token.IsAdmin;
        if (isAdmin) claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(principal, new AuthenticationProperties { IsPersistent = true });

        return Ok(new AccountInfo { Id = studentId, Name = studentName.Value, IsAdmin = isAdmin, Token = token.Id });
    }

    [HttpGet]
    [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
    public IActionResult GetClaims()
    {
        return Ok(User.Claims.ToDictionary(x => x.Type, x => x.Value));
    }
}

public class AccountInfo
{
    [Required]
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = null!;
    [Required]
    public bool IsAdmin { get; set; }
    public string? Token { get; set; }
}
