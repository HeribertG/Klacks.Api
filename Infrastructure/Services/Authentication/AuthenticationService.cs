using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Presentation.DTOs;
using Klacks.Api.Application.Validation.Accounts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;

namespace Klacks.Api.Infrastructure.Services.Authentication;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly JwtValidator _jwtValidator;
    private readonly IIdentityProviderRepository _identityProviderRepository;
    private readonly ILdapService _ldapService;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        UserManager<AppUser> userManager,
        JwtValidator jwtValidator,
        IIdentityProviderRepository identityProviderRepository,
        ILdapService ldapService,
        ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;
        _jwtValidator = jwtValidator;
        _identityProviderRepository = identityProviderRepository;
        _ldapService = ldapService;
        _logger = logger;
    }

    public async Task<(bool IsValid, AppUser? User)> ValidateCredentialsAsync(string email, string password)
    {
        _logger.LogInformation("ValidateCredentialsAsync called for: {Email}", email);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Empty email or password");
            return (false, null);
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user != null && await _userManager.CheckPasswordAsync(user, password))
        {
            _logger.LogInformation("Local authentication successful for: {Email}", email);
            return (true, user);
        }

        _logger.LogInformation("Local authentication failed, trying LDAP for: {Email}", email);
        var ldapResult = await ValidateLdapCredentialsAsync(email, password);
        if (ldapResult.IsValid)
        {
            _logger.LogInformation("LDAP authentication successful for: {Email}", email);
            return ldapResult;
        }

        _logger.LogWarning("All authentication methods failed for: {Email}", email);
        return (false, null);
    }

    private async Task<(bool IsValid, AppUser? User)> ValidateLdapCredentialsAsync(string username, string password)
    {
        var providers = await _identityProviderRepository.GetAuthenticationProviders();
        _logger.LogInformation("Found {Count} LDAP authentication providers", providers.Count);

        foreach (var provider in providers)
        {
            _logger.LogInformation("Trying LDAP provider: {Provider} ({Host}:{Port})", provider.Name, provider.Host, provider.Port);
            try
            {
                var isValid = await _ldapService.ValidateCredentialsAsync(provider, username, password);
                _logger.LogInformation("LDAP validation result for {Provider}: {IsValid}", provider.Name, isValid);

                if (isValid)
                {
                    _logger.LogInformation("LDAP authentication successful for user {Username} via provider {Provider}", username, provider.Name);

                    var user = await GetOrCreateLdapUserAsync(username, provider);
                    if (user != null)
                    {
                        return (true, user);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LDAP authentication failed for provider {Provider}", provider.Name);
            }
        }

        return (false, null);
    }

    private async Task<AppUser?> GetOrCreateLdapUserAsync(string username, IdentityProvider provider)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user != null)
        {
            return user;
        }

        user = await _userManager.FindByEmailAsync(username);
        if (user != null)
        {
            return user;
        }

        var newUser = new AppUser
        {
            UserName = username,
            Email = username.Contains('@') ? username : $"{username}@ldap.local",
            EmailConfirmed = true,
            FirstName = username,
            LastName = string.Empty
        };

        var result = await _userManager.CreateAsync(newUser);
        if (result.Succeeded)
        {
            _logger.LogInformation("Created new LDAP user {Username} for provider {Provider}", username, provider.Name);
            return newUser;
        }

        _logger.LogError("Failed to create LDAP user {Username}: {Errors}", username, string.Join(", ", result.Errors.Select(e => e.Description)));
        return null;
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

    public async Task<(bool Success, IdentityResult? Result)> ResetPasswordAsync(AppUser user, string token, string newPassword)
    {
        try
        {
            // For our custom token system, we'll change the password directly
            var passwordHasher = new PasswordHasher<AppUser>();
            user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
            
            var result = await _userManager.UpdateAsync(user);
            return (result.Succeeded, result);
        }
        catch
        {
            return (false, null);
        }
    }
}