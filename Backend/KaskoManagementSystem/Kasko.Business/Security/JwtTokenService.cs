using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kasko.Entities.Concrete;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Kasko.Business.Security;

public class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime Expiration) GenerateToken(User user, string roleName)
    {
        var jwtSettings = _configuration.GetSection("Jwt");

        var key = jwtSettings["Key"]
            ?? throw new InvalidOperationException("JWT Key bulunamadı.");

        var issuer = jwtSettings["Issuer"]
            ?? throw new InvalidOperationException("JWT Issuer bulunamadı.");

        var audience = jwtSettings["Audience"]
            ?? throw new InvalidOperationException("JWT Audience bulunamadı.");

        var expireMinutes = int.Parse(
            jwtSettings["ExpireMinutes"] ?? "60");

        var expiration = DateTime.UtcNow.AddMinutes(expireMinutes);

        var claims = new List<Claim>
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new Claim(
                JwtRegisteredClaimNames.Email,
                user.Email),

            new Claim(
                ClaimTypes.Name,
                $"{user.FirstName} {user.LastName}"),

            new Claim(
                ClaimTypes.Role,roleName),

            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key));

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiration,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return (tokenString, expiration);
    }
}