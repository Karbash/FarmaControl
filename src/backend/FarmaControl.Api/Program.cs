using FarmaControl.Api.Middleware;
using FarmaControl.Api.Auth;
using FarmaControl.Application.Abstractions;
using FarmaControl.Application;
using FarmaControl.Application.Users.Abstractions;
using FarmaControl.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

const string AngularDevCorsPolicy = "AngularDev";

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
string[] corsOrigins = ResolveCorsOrigins(builder.Configuration, builder.Environment);
if (corsOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(
            AngularDevCorsPolicy,
            policy => policy
                .WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod());
    });
}

builder.Services.AddMemoryCache();
var jwtTokenService = new JwtTokenService(builder.Configuration, builder.Environment);
builder.Services.AddSingleton(jwtTokenService);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtTokenService.Issuer(),
            ValidateAudience = true,
            ValidAudience = jwtTokenService.Audience(),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtTokenService.SigningKey())),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                string? value = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!long.TryParse(value, out long userId))
                {
                    context.Fail("Usuario invalido.");
                    return;
                }

                var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
                string cacheKey = $"auth:user:{userId}:can-authenticate";
                bool canAuthenticate = await cache.GetOrCreateAsync(
                    cacheKey,
                    async entry =>
                    {
                        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                        var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                        var user = await users.GetByIdAsync(userId, context.HttpContext.RequestAborted);
                        return user?.CanAuthenticate == true;
                    });

                if (!canAuthenticate)
                {
                    context.Fail("Usuario sem acesso ativo.");
                    return;
                }

                if (context.Principal?.Identity is ClaimsIdentity identity)
                {
                    string? role = identity.FindFirst(ClaimTypes.Role)?.Value;
                    if (string.Equals(role, "atendimento", StringComparison.OrdinalIgnoreCase))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, "atendente"));
                    }

                    if (string.Equals(role, "enfermagem", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(role, "enfermeiro", StringComparison.OrdinalIgnoreCase))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, "enfermeira"));
                    }
                }
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IInitialDataSeeder>();
    await seeder.SeedAsync(CancellationToken.None);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment() &&
    builder.Configuration.GetValue("Https:Redirect", false))
{
    app.UseHttpsRedirection();
}

if (corsOrigins.Length > 0)
{
    app.UseCors(AngularDevCorsPolicy);
}
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/api/health"));
app.MapControllers();

app.Run();

static string[] ResolveCorsOrigins(IConfiguration configuration, IHostEnvironment environment)
{
    string[] configuredOrigins = configuration
        .GetSection("Cors:AllowedOrigins")
        .GetChildren()
        .Select(section => section.Value)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value!.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (configuredOrigins.Length > 0)
    {
        return configuredOrigins;
    }

    return environment.IsDevelopment()
        ? ["http://localhost:4200", "http://127.0.0.1:4200"]
        : [];
}
