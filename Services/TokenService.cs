using Klacks.Api.Constants;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Models.Authentification;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Klacks.Api.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly UserManager<AppUser> _userManager;

    public TokenService(UserManager<AppUser> userManager, JwtSettings jwtSettings)
    {
        _userManager = userManager;
        _jwtSettings = jwtSettings;
    }

    public async Task<string> CreateToken(AppUser user, DateTime expires)
    {
        List<Claim> claims = new List<Claim>()
        {
          new Claim(ClaimTypes.NameIdentifier, user.Id),
          new Claim(ClaimTypes.Email, user.Email ?? ""),
          new Claim(ClaimTypes.Name, user.UserName ?? ""),
          new Claim(ClaimTypes.GivenName, user.FirstName),
          new Claim(ClaimTypes.Surname, user.LastName),
          new Claim("jti", Guid.NewGuid().ToString()),
          new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var userRoles = await GetUserRoles(user);
        foreach (var item in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, item));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

        var token = new JwtSecurityToken(
          issuer: _jwtSettings.ValidIssuer,
          audience: _jwtSettings.ValidAudience,
          claims: claims,
          expires: expires,
          notBefore: DateTime.UtcNow,
          signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return jwt;
    }

    private async Task<List<string>> GetUserRoles(AppUser user)
    {
        return (await _userManager.GetRolesAsync(user)).ToList();
    }
}
