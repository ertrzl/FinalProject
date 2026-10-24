using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Services;

public class TokenService : ITokenService
{
    public const string SecurityStampClaim = "stamp";

    public const string SessionClaim = "sid";

    private const int DefaultExpiryMinutes = 15;

    private const int DefaultMediaCookieHours = 12;

    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TokenResult GenerateAccessToken(Guid userId, string email, string fullName, string securityStamp, Guid sessionId, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, fullName),
            new(SecurityStampClaim, securityStamp),
            new(SessionClaim, sessionId.ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret.Read(_configuration)));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? DefaultExpiryMinutes.ToString());

        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public static string MediaAudience(IConfiguration configuration) => $"{configuration["Jwt:Audience"]}.media";

    public TokenResult GenerateMediaToken(Guid userId, string securityStamp, Guid? sessionId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(SecurityStampClaim, securityStamp)
        };

        if (sessionId.HasValue)
            claims.Add(new Claim(SessionClaim, sessionId.Value.ToString()));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret.Read(_configuration)));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var hours = double.Parse(_configuration["Jwt:MediaCookieHours"] ?? DefaultMediaCookieHours.ToString(), System.Globalization.CultureInfo.InvariantCulture);
        var expiresAt = DateTime.UtcNow.AddHours(hours);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: MediaAudience(_configuration),
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
