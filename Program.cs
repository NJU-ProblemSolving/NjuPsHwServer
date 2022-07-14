using Microsoft.Extensions.Caching.Memory;

using NjuCsCmsHelper.Models;
using NjuCsCmsHelper.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppConfiguration(config =>
{
    config.AddJsonFile("data/appsettings.json", optional: true);
});

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(
        builder.Configuration.GetConnectionString("Main"),
        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
    );
    // options.LogTo(Console.WriteLine);
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

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme) // Sets the default scheme to cookies
    .AddCookie(o =>
    {
        o.ExpireTimeSpan = TimeSpan.FromDays(365);
        o.SlidingExpiration = true;
    })
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = builder.Configuration["OpenIdConnect:Authority"];
        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("OpenIdConnect:RequireHttpsMetadata");

        options.ClientId = builder.Configuration["OpenIdConnect:ClientId"];
        options.ClientSecret = builder.Configuration["OpenIdConnect:ClientSecret"];
        options.ResponseType = "code";

        options.SaveTokens = true;
        options.Scope.Add("studentInfo");
        options.GetClaimsFromUserInfoEndpoint = builder.Configuration.GetValue<bool>("OpenIdConnect:RequireHttpsMetadata");
        
        options.ClaimActions.MapJsonKey("studentId", "studentId");
        options.ClaimActions.MapJsonKey("role", "role");
    });

builder.Services.AddSingleton<IAuthorizationHandler, MyAuthorizationHandler>();
builder.Services.AddAuthorization();

builder.Services.AddSingleton<IMemoryCache, MemoryCache>();
builder.Services.AddScoped<IMyAppService, MyAppService>();
builder.Services.AddScoped<SubmissionService>();
builder.Services.AddScoped<MailingService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
