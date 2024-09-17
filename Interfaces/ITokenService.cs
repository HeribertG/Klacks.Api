using Klacks_api.Models.Authentification;

namespace Klacks_api.Interfaces;

public interface ITokenService
{
  Task<string> CreateToken(AppUser user, DateTime expires);
}
