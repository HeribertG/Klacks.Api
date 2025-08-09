using Klacks.Api.Interfaces.Domains;
using Klacks.Api.Models.Authentification;
using Klacks.Api.Presentation.Resources;
using Klacks.Api.Validation.Accounts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;

namespace Klacks.Api.Services.Authentication;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly JwtValidator _jwtValidator;

    public AuthenticationService(UserManager<AppUser> userManager, JwtValidator jwtValidator)
    {
        _userManager = userManager;
        _jwtValidator = jwtValidator;
    }

    public async Task<(bool IsValid, AppUser? User)> ValidateCredentialsAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return (false, null);
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user != null && await _userManager.CheckPasswordAsync(user, password))
        {
            return (true, user);
        }

        return (false, null);
    }

    public async Task<(bool Success, IdentityResult? Result)> ChangePasswordAsync(AppUser user, string oldPassword, string newPassword)
    {
        try
        {
            var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
            return (result.Succeeded, result);
        }
        catch
        {
            return (false, null);
        }
    }

    public async Task<AppUser?> GetUserFromAccessTokenAsync(string token)
    {
        try
        {
            var principal = _jwtValidator.ValidateToken(token);
            if (principal == null)
            {
                return null;
            }

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return null;
            }

            return await _userManager.FindByIdAsync(userIdClaim.Value);
        }
        catch
        {
            return null;
        }
    }

    public ModelStateDictionary AddErrorsToModelState(IdentityResult identityResult, ModelStateDictionary? modelState = null)
    {
        modelState ??= new ModelStateDictionary();

        foreach (var error in identityResult.Errors)
        {
            modelState.TryAddModelError(error.Code, error.Description);
        }

        return modelState;
    }

    public void SetModelError(AuthenticatedResult model, string key, string message)
    {
        model.ModelState ??= new ModelStateDictionary();
        model.ModelState.TryAddModelError(key, message);
    }
}