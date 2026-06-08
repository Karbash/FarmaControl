using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FarmaControl.Contracts.Auth;
using Microsoft.IdentityModel.Tokens;

namespace FarmaControl.Api.Auth;

public sealed record JwtTokenResult(string AccessToken, DateTimeOffset ExpiresAt);

public sealed class JwtTokenService(
    IConfiguration configuration,
    IHostEnvironment environment)
{
    private const string DefaultIssuer = "FarmaControl";
    private const string DefaultAudience = "FarmaControl.Angular";
    private const int DefaultExpirationMinutes = 480;
    private const string DevelopmentKey = "farmacontrol-development-signing-key-change-in-production-2026";

    public JwtTokenResult CreateToken(AuthenticatedUserResponse user)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset expiresAt = now.AddMinutes(ExpirationMinutes());

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        claims.AddRange(user.Modules.Select(module => new Claim("module", module)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey())),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer(),
            audience: Audience(),
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtTokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }

    public string Issuer()
    {
        return configuration["Jwt:Issuer"] ?? DefaultIssuer;
    }

    public string Audience()
    {
        return configuration["Jwt:Audience"] ?? DefaultAudience;
    }

    public string SigningKey()
    {
        string? configuredKey = configuration["Jwt:Key"];
        if (!string.IsNullOrWhiteSpace(configuredKey))
        {
            return configuredKey;
        }

        if (environment.IsDevelopment())
        {
            return DevelopmentKey;
        }

        throw new InvalidOperationException("Jwt:Key deve ser configurado fora do ambiente de desenvolvimento.");
    }

    private int ExpirationMinutes()
    {
        return int.TryParse(configuration["Jwt:AccessTokenMinutes"], out int minutes)
            ? minutes
            : DefaultExpirationMinutes;
    }
}
