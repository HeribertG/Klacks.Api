using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Presentation.DTOs;
using Klacks.Api.Presentation.DTOs.Registrations;

namespace Klacks.Api.Domain.Services.Accounts;

public interface IAccountManagementService
{
    Task<HttpResultResource> ChangeRoleUserAsync(ChangeRole editUserRole);
    Task<HttpResultResource> DeleteAccountUserAsync(Guid id);
    Task<List<UserResource>> GetUserListAsync();
}