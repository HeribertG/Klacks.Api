using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Services.Accounts;

public interface IAccountRegistrationService
{
    Task<AuthenticatedResult> RegisterUserAsync(AppUser user, string password);
}