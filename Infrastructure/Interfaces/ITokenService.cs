using Klacks.Api.Models.Authentification;

namespace Klacks.Api.Infrastructure.Interfaces;

public interface ITokenService
{
    Task<string> CreateToken(AppUser user, DateTime expires);
}
