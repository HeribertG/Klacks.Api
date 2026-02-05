using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Application.DTOs;
using Klacks.Api.Application.DTOs.Registrations;

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
        this._logger = logger;
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

    public async Task<HttpResultResource> UpdateAccountAsync(UpdateAccountResource updateAccount)
    {
        _logger.LogInformation("Updating user account {UserId}", updateAccount.Id);

        var user = await _userManagementService.FindUserByIdAsync(updateAccount.Id.ToString());
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", updateAccount.Id);
            return new HttpResultResource
            {
                Success = false,
                Messages = "User not found"
            };
        }

        user.FirstName = updateAccount.FirstName;
        user.LastName = updateAccount.LastName;
        user.UserName = updateAccount.UserName;
        user.Email = updateAccount.Email;

        var (success, result) = await _userManagementService.UpdateUserAsync(user);

        if (success)
        {
            _logger.LogInformation("User account {UserId} updated successfully", updateAccount.Id);
        }
        else
        {
            var errors = result?.Errors?.Select(e => e.Description) ?? [];
            _logger.LogWarning("Failed to update user account {UserId}: {Errors}", updateAccount.Id, string.Join(", ", errors));
        }

        return new HttpResultResource
        {
            Success = success,
            Messages = success ? "Account updated" : string.Join(", ", result?.Errors?.Select(e => e.Description) ?? [])
        };
    }
}