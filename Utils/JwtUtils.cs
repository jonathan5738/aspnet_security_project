using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace SecurityProject.Utils;

public class JwtUtils
{
    public static string GenerateToken(IConfiguration config, string role, int userId)
    {
        var secret = config.GetValue<string>("JwtSecret")!;

        var key = new SymmetricSecurityKey(Convert.FromBase64String(secret));
        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var issuer = config.GetValue<string>("JwtIssuer")!;
        var validAudience = config.GetValue<string>("JwtAudience"); 

        var claims = new []
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: validAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(10),
            signingCredentials: signingCredentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}