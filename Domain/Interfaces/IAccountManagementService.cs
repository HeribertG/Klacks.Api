using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Presentation.DTOs;
using Klacks.Api.Presentation.DTOs.Registrations;

namespace Klacks.Api.Domain.Interfaces;

public interface IAccountManagementService
{
    Task<HttpResultResource> ChangeRoleUserAsync(ChangeRole editUserRole);

    Task<HttpResultResource> DeleteAccountUserAsync(Guid id);

    Task<List<UserResource>> GetUserListAsync();

    Task<HttpResultResource> UpdateAccountAsync(UpdateAccountResource updateAccount);
}