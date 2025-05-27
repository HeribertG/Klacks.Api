using Klacks.Api.Constants;
using Klacks.Api.Interfaces;
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
    var roles = new List<string>();
    var isAdmin = await _userManager.IsInRoleAsync(user, Roles.Admin);
    var isAuthorised = await _userManager.IsInRoleAsync(user, Roles.Authorised);

    if (isAdmin)
    {
      roles.Add(Roles.Admin);
    }

    if (isAuthorised)
    {
      roles.Add(Roles.Authorised);
    }

    List<Claim> claims = new List<Claim>()
      {
          new Claim(ClaimTypes.NameIdentifier, user.UserName!),
          new Claim(ClaimTypes.Email, user.Email!),
          new Claim(ClaimTypes.GivenName, user.FirstName ),
          new Claim(ClaimTypes.Surname, user.LastName),
          new Claim(ClaimTypes.Role, user.LastName),
          new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
          new Claim("Id", user.Id ),
      };

    foreach (var item in roles)
    {
      claims.Add(new Claim(ClaimTypes.Role, item));
    }

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

    var token = new JwtSecurityToken(
      issuer: _jwtSettings.ValidIssuer,
      audience: _jwtSettings.ValidAudience,
      claims: claims,
      expires: DateTime.UtcNow.AddMinutes(1),
      notBefore: DateTime.UtcNow,
      signingCredentials: creds);

    var jwt = new JwtSecurityTokenHandler().WriteToken(token);

    return jwt;
  }
}
