using Klacks.Api.Domain.Models.Authentification;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Klacks.Api.Application.Validation.Accounts;

public class JwtValidator
{
    private readonly JwtSettings _jwtSettings;

    public JwtValidator(JwtSettings jwtSettings)
    {
        _jwtSettings = jwtSettings;
    }

    public ClaimsPrincipal ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = !string.IsNullOrEmpty(_jwtSettings.ValidIssuer),
            ValidIssuer = _jwtSettings.ValidIssuer,
            ValidateAudience = !string.IsNullOrEmpty(_jwtSettings.ValidAudience),
            ValidAudience = _jwtSettings.ValidAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);

            if (!(validatedToken is JwtSecurityToken jwtSecurityToken) ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }
        catch (Exception ex)
        {
            throw new SecurityTokenException("Token validation failed.");
        }
    }
}