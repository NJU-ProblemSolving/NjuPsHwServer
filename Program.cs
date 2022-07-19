using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Security.Cryptography.X509Certificates;

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

var authBuilder = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);
authBuilder.AddCookie(o =>
    {
        o.ExpireTimeSpan = TimeSpan.FromDays(365);
        o.SlidingExpiration = true;
    });

var defaultPolicyBuilder = new AuthorizationPolicyBuilder(CookieAuthenticationDefaults.AuthenticationScheme);
defaultPolicyBuilder.RequireAuthenticatedUser();

if (builder.Configuration.GetSection("Jwt").Exists())
{
    authBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new X509SecurityKey(new X509Certificate2(
                builder.Configuration["Jwt:Certificate"]
            ))
        };
    });
    defaultPolicyBuilder.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
}

if (builder.Configuration.GetSection("OpenIdConnect").Exists())
{
    authBuilder.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
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
    defaultPolicyBuilder.AddAuthenticationSchemes(OpenIdConnectDefaults.AuthenticationScheme);
}

builder.Services.AddSingleton<IAuthorizationHandler, MyAuthorizationHandler>();
builder.Services.AddAuthorization(o =>
{
    var defaultPolicy = defaultPolicyBuilder.Build();
    o.AddPolicy("Default", defaultPolicy);
    o.AddPolicy("Student", p => p.Combine(defaultPolicy).AddRequirements(OwnerOrAdminRequirement.Instance));
    o.AddPolicy("Reviewer", p => p.Combine(defaultPolicy).RequireClaim("role", "Admin"));
    o.AddPolicy("Admin", p => p.Combine(defaultPolicy).RequireClaim("role", "Admin"));
    o.DefaultPolicy = defaultPolicy;
});

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
