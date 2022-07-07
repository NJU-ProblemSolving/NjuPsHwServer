using Microsoft.Extensions.Caching.Memory;
using NjuCsCmsHelper.Models;
using NjuCsCmsHelper.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(
    options =>
    {
        options.UseSqlite(builder.Configuration.GetConnectionString("Main"),
                                 o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
        options.LogTo(Console.WriteLine);
    });

// Learn more about configuring Swagger/OpenAPI at
// https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

Func<RedirectContext<CookieAuthenticationOptions>, Task> ReplaceRedirector(
    int statusCode, Func<RedirectContext<CookieAuthenticationOptions>, Task> existingRedirector) => context =>
{
    if (context.Request.Path.StartsWithSegments("/api") && context.Response.StatusCode == 200)
    {
        context.Response.StatusCode = statusCode;
        return Task.CompletedTask;
    }
    return existingRedirector(context);
};
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme) // Sets the default scheme to cookies
    .AddCookie(o =>
    {
        o.ExpireTimeSpan = TimeSpan.FromDays(365);
        o.SlidingExpiration = true;
        o.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = ReplaceRedirector(StatusCodes.Status401Unauthorized, o.Events.OnRedirectToAccessDenied),
            OnRedirectToAccessDenied =
                ReplaceRedirector(StatusCodes.Status403Forbidden, o.Events.OnRedirectToAccessDenied),
        };
    });

builder.Services.AddSingleton<IAuthorizationHandler, MyAuthorizationHandler>();
builder.Services.AddAuthorization();

builder.Services.AddSingleton<IMemoryCache, MemoryCache>();
builder.Services.AddScoped<IMyAppService, MyAppService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseFileServer();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
