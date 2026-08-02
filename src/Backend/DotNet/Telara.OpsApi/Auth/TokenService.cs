using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Telara.Domain.Entities;

namespace Telara.OpsApi.Auth;

public class TokenService(IConfiguration configuration)
{
    public int RefreshTokenLifetimeDays => configuration.GetValue("Jwt:RefreshTokenDays", 14);

    public (string AccessToken, DateTime ExpiresAtUtc) GenerateAccessToken(User user)
    {
        var minutes = configuration.GetValue("Jwt:AccessTokenMinutes", 15);
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(minutes);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SigningKey"]!));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
        };

        if (user.Role is not null)
            claims.Add(new Claim(ClaimTypes.Role, user.Role.Name));

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }

    public static string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
