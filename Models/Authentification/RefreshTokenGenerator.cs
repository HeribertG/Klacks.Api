using System.Security.Cryptography;

namespace Klacks.Api.Models.Authentification;

public class RefreshTokenGenerator
{
  public string GenerateRefreshToken()
  {
    var randomNumber = new byte[32];
    using (var rng = RandomNumberGenerator.Create())
    {
      rng.GetBytes(randomNumber);
      return Convert.ToBase64String(randomNumber);
    }
  }
}
