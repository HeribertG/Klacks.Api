using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Interfaces;

public interface IAccountRegistrationService
{
    Task<AuthenticatedResult> RegisterUserAsync(AppUser user, string password);
}