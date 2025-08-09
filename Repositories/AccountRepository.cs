using Klacks.Api.Constants;
using Klacks.Api.Datas;
using Klacks.Api.Helper;
using Klacks.Api.Helper.Email;
using Klacks.Api.Interfaces;
using Klacks.Api.Interfaces.Domains;
using Klacks.Api.Models.Authentification;
using Klacks.Api.Presentation.DTOs;
using Klacks.Api.Presentation.DTOs.Registrations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Klacks.Api.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly DataBaseContext appDbContext;
    private readonly ITokenService tokenService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserManagementService _userManagementService;
    private readonly IRefreshTokenService _refreshTokenService;

    public AccountRepository(DataBaseContext appDbContext,
                             ITokenService tokenService,
                             IAuthenticationService authenticationService,
                             IUserManagementService userManagementService,
                             IRefreshTokenService refreshTokenService)
    {
        this.appDbContext = appDbContext;
        this.tokenService = tokenService;
        _authenticationService = authenticationService;
        _userManagementService = userManagementService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<AuthenticatedResult> ChangePassword(ChangePasswordResource model)
    {
        return await ChangePasswordUser(model);
    }

    public async Task<AuthenticatedResult> ChangePasswordUser(ChangePasswordResource model)
    {
        var authenticatedResult = new AuthenticatedResult { Success = false };

        var existingUser = await _userManagementService.FindUserByEmailAsync(model.Email);

        if (existingUser == null)
        {
            _authenticationService.SetModelError(authenticatedResult, "The user does not exist", "There is no user with this e-mail address.");
            return authenticatedResult;
        }

        var (success, result) = await _authenticationService.ChangePasswordAsync(existingUser, model.OldPassword, model.Password);
        if (success)
        {
            authenticatedResult.Success = true;
        }
        else if (result != null)
        {
            authenticatedResult.ModelState = _authenticationService.AddErrorsToModelState(result, authenticatedResult.ModelState);
        }
        else
        {
            _authenticationService.SetModelError(authenticatedResult, "An unexpected error has occurred.", string.Empty);
        }

        return authenticatedResult;
    }

    /// <summary>
    /// Changes the role of a user.
    /// </summary>
    /// <param name="editUserRole">The information on the role change.</param>
    public async Task<HttpResultResource> ChangeRoleUser(ChangeRole editUserRole)
    {
        var (success, message) = await _userManagementService.ChangeUserRoleAsync(
            editUserRole.UserId, editUserRole.RoleName, editUserRole.IsSelected);

        return new HttpResultResource
        {
            Success = success,
            Messages = message
        };
    }

    public async Task<HttpResultResource> DeleteAccountUser(Guid id)
    {
        var (success, message) = await _userManagementService.DeleteUserAsync(id);

        return new HttpResultResource
        {
            Success = success,
            Messages = message
        };
    }

    public async Task<List<UserResource>> GetUserList()
    {
        return await _userManagementService.GetUserListAsync();
    }

    public async Task<AuthenticatedResult> LogInUser(string email, string password)
    {
        var authenticatedResult = new AuthenticatedResult { Success = false };
        
        var (isValid, user) = await _authenticationService.ValidateCredentialsAsync(email, password);
        
        if (!isValid || user == null)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                _authenticationService.SetModelError(authenticatedResult, "Login failed", "The e-mail and password must not be empty.");
            }
            else
            {
                _authenticationService.SetModelError(authenticatedResult, "Login failed", "The login details are not correct.");
            }
            return authenticatedResult;
        }

        try
        {
            return await GenerateAuthentication(user);
        }
        catch (Exception ex)
        {
            _authenticationService.SetModelError(authenticatedResult, "Login failed", ex.Message);
            return authenticatedResult;
        }
    }

    public async Task<AuthenticatedResult> RefreshToken(RefreshRequestResource model)
    {
        var authenticatedResult = new AuthenticatedResult { Success = false };

        if (model == null)
        {
            _authenticationService.SetModelError(authenticatedResult, "RefreshTokenError", "Refresh request model cannot be null.");
            return authenticatedResult;
        }

        // Try to get user from access token first
        var user = await _authenticationService.GetUserFromAccessTokenAsync(model.Token);

        // If that fails, try via refresh token directly
        if (user == null)
        {
            user = await _refreshTokenService.GetUserFromRefreshTokenAsync(model.RefreshToken);
        }

        if (user == null)
        {
            _authenticationService.SetModelError(authenticatedResult, "RefreshTokenError", "Token invalid or expired.");
            return authenticatedResult;
        }

        try
        {
            if (await _refreshTokenService.ValidateRefreshTokenAsync(user.Id, model.RefreshToken))
            {
                return await GenerateAuthentication(user, true);
            }
        }
        catch (Exception ex)
        {
            _authenticationService.SetModelError(authenticatedResult, "RefreshTokenError", "An error occurred while refreshing the token.");
            Console.WriteLine($"RefreshToken Error: {ex.Message}");
            return authenticatedResult;
        }

        _authenticationService.SetModelError(authenticatedResult, "RefreshTokenError", "Invalid refresh token.");
        return authenticatedResult;
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="user">Username of the user.</param>
    /// <param name="password">His password.</param>
    public async Task<AuthenticatedResult> RegisterUser(AppUser user, string password)
    {
        var authenticatedResult = new AuthenticatedResult { Success = false };

        var (success, result) = await _userManagementService.RegisterUserAsync(user, password);
        
        if (!success)
        {
            if (result != null)
            {
                authenticatedResult.ModelState = _authenticationService.AddErrorsToModelState(result, authenticatedResult.ModelState);
            }
            else
            {
                _authenticationService.SetModelError(authenticatedResult, "Registration failed", "User registration failed. Please check your input.");
            }
            return authenticatedResult;
        }

        var expires = _refreshTokenService.CalculateTokenExpiryTime();
        authenticatedResult = await SetAuthenticatedResult(authenticatedResult, user, expires);

        await appDbContext.SaveChangesAsync();

        return authenticatedResult;
    }

    public Task<AuthenticatedResult> ResetPassword(ResetPasswordResource data)
    {
        throw new NotImplementedException();
    }

    public Task<string> SendEmail(string title, string email, string message)
    {
        var mail = new MsgEMail(appDbContext);
        return Task.FromResult(mail.SendMail(email, title, message));
    }

    public void SetModelError(AuthenticatedResult model, string key, string message)
    {
        _authenticationService.SetModelError(model, key, message);
    }

    public async Task<bool> ValidateRefreshTokenAsync(AppUser user, string refreshToken)
    {
        return await _refreshTokenService.ValidateRefreshTokenAsync(user.Id, refreshToken);
    }


    private async Task<AuthenticatedResult> GenerateAuthentication(AppUser user, bool withRefreshToken = true)
    {
        var authenticatedResult = new AuthenticatedResult { Success = false };
        var expires = _refreshTokenService.CalculateTokenExpiryTime();

        if (withRefreshToken)
        {
            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(user.Id);
            authenticatedResult.RefreshToken = refreshToken;
        }

        authenticatedResult = await SetAuthenticatedResult(authenticatedResult, user, expires);

        return authenticatedResult;
    }


    private async Task<AuthenticatedResult> SetAuthenticatedResult(AuthenticatedResult authenticatedResult, AppUser user, DateTime expires)
    {
        authenticatedResult.Token = await tokenService.CreateToken(user, expires);
        authenticatedResult.Success = true;
        authenticatedResult.Expires = expires;
        authenticatedResult.UserName = user.UserName!;
        authenticatedResult.FirstName = user.FirstName;
        authenticatedResult.Name = user.LastName;
        authenticatedResult.Id = user.Id;
        authenticatedResult.IsAdmin = await _userManagementService.IsUserInRoleAsync(user, Roles.Admin);
        authenticatedResult.IsAuthorised = await _userManagementService.IsUserInRoleAsync(user, Roles.Authorised);

        return authenticatedResult;
    }



}
