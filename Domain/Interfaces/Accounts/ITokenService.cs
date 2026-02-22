using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Interfaces;

public interface ITokenService
{
    Task<string> CreateToken(AppUser user, DateTime expires);
}
