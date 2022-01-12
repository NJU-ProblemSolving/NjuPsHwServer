using System.Reflection;
using System.Text.Unicode;
using System.Text.Encodings.Web;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Caching.Memory;
using NjuCsCmsHelper.Models;

const string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    if (builder.Configuration["CertPath"] == null)
        throw new ArgumentNullException("Certificate path not configured in appsettings");
    builder.WebHost.UseKestrel(
        o => o.ConfigureHttpsDefaults(o => o.ServerCertificate = X509Certificate2.CreateFromPemFile(
                                          builder.Configuration["CertPath"], builder.Configuration["CertKeyPath"])));
}

builder.Services.AddControllers().AddJsonOptions(options => {
    options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
});

builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlite(builder.Configuration["DbString"],
                                 o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

// Learn more about configuring Swagger/OpenAPI at
// https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

builder.Services.AddCors(options => options.AddPolicy(name: MyAllowSpecificOrigins, builder => {
    builder.WithOrigins("http://localhost:3000").AllowAnyMethod().AllowAnyHeader();
}));

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
    .AddCookie(o => {
        o.Events = new CookieAuthenticationEvents {
            OnRedirectToLogin = ReplaceRedirector(StatusCodes.Status401Unauthorized, o.Events.OnRedirectToAccessDenied),
            OnRedirectToAccessDenied =
                ReplaceRedirector(StatusCodes.Status403Forbidden, o.Events.OnRedirectToAccessDenied),
        };
    });

builder.Services.AddSingleton<IMemoryCache, MemoryCache>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseFileServer();

app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
