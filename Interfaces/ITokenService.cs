using Klacks.Api.Models.Authentification;

namespace Klacks.Api.Interfaces;

public interface ITokenService
{
  Task<string> CreateToken(AppUser user, DateTime expires);
}
