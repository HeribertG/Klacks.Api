using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Presentation.DTOs;
using Klacks.Api.Presentation.DTOs.Registrations;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Domain.Services.Accounts;

public class AccountManagementService : IAccountManagementService
{
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<AccountManagementService> _logger;

    public AccountManagementService(
        IUserManagementService userManagementService,
        ILogger<AccountManagementService> logger)
    {
        _userManagementService = userManagementService;
        _logger = logger;
    }

    public async Task<HttpResultResource> ChangeRoleUserAsync(ChangeRole editUserRole)
    {
        _logger.LogInformation("Changing role for user {UserId} to {RoleName}: {IsSelected}", 
            editUserRole.UserId, editUserRole.RoleName, editUserRole.IsSelected);

        var (success, message) = await _userManagementService.ChangeUserRoleAsync(
            editUserRole.UserId, editUserRole.RoleName, editUserRole.IsSelected);

        if (success)
        {
            _logger.LogInformation("Role change successful for user {UserId}", editUserRole.UserId);
        }
        else
        {
            _logger.LogWarning("Role change failed for user {UserId}: {Message}", editUserRole.UserId, message);
        }

        return new HttpResultResource
        {
            Success = success,
            Messages = message
        };
    }

    public async Task<HttpResultResource> DeleteAccountUserAsync(Guid id)
    {
        _logger.LogInformation("Deleting user account {UserId}", id);

        var (success, message) = await _userManagementService.DeleteUserAsync(id);

        if (success)
        {
            _logger.LogInformation("User account {UserId} deleted successfully", id);
        }
        else
        {
            _logger.LogWarning("Failed to delete user account {UserId}: {Message}", id, message);
        }

        return new HttpResultResource
        {
            Success = success,
            Messages = message
        };
    }

    public async Task<List<UserResource>> GetUserListAsync()
    {
        _logger.LogDebug("Retrieving user list");
        return await _userManagementService.GetUserListAsync();
    }
}