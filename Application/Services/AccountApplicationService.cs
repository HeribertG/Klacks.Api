using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Presentation.DTOs;
using Klacks.Api.Presentation.DTOs.Registrations;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services;

public class AccountApplicationService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<AccountApplicationService> _logger;

    public AccountApplicationService(
        IAccountRepository accountRepository,
        IMapper mapper,
        ILogger<AccountApplicationService> logger)
    {
        _accountRepository = accountRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<TokenResource?> LoginUserAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Login attempt for user: {Email}", email);
        
        var result = await _accountRepository.LogInUser(email, password);
        
        if (result != null && result.Success)
        {
            var response = new TokenResource
            {
                Success = true,
                Token = result.Token,
                Username = result.UserName,
                FirstName = result.FirstName,
                Name = result.Name,
                Id = result.Id,
                ExpTime = result.Expires,
                IsAdmin = result.IsAdmin,
                IsAuthorised = result.IsAuthorised,
                RefreshToken = result.RefreshToken,
                Version = new MyVersion().Get(),
                Subject = email
            };
            
            _logger.LogInformation("Login successful for user: {Email}", email);
            return response;
        }
        
        _logger.LogWarning("Login failed for user: {Email}", email);
        return null;
    }

    public async Task<AuthenticatedResult> RegisterUserAsync(RegistrationResource registrationResource, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User registration requested: {Email}", registrationResource.Email);
        
        var userIdentity = _mapper.Map<AppUser>(registrationResource);
        var result = await _accountRepository.RegisterUser(userIdentity, registrationResource.Password);
        
        if (result != null && result.Success)
        {
            _logger.LogInformation("User registration successful: {Email}", registrationResource.Email);
        }
        else
        {
            _logger.LogWarning("User registration failed: {Email}, Reason: {Reason}", 
                registrationResource.Email, result?.Message ?? "Unknown");
        }
        
        return result;
    }

    public async Task<TokenResource?> RefreshTokenAsync(RefreshRequestResource refreshRequest, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RefreshToken requested");
        
        var result = await _accountRepository.RefreshToken(refreshRequest);
        
        if (result != null && result.Success)
        {
            var response = new TokenResource
            {
                Success = true,
                Token = result.Token,
                Username = result.UserName,
                FirstName = result.FirstName,
                Name = result.Name,
                Id = result.Id,
                ExpTime = result.Expires,
                IsAdmin = result.IsAdmin,
                IsAuthorised = result.IsAuthorised,
                RefreshToken = result.RefreshToken,
                Version = new MyVersion().Get()
            };
            
            _logger.LogInformation("Token refresh successful for user: {UserId}", result.Id);
            return response;
        }
        
        _logger.LogWarning("Token refresh failed");
        return null;
    }

    public async Task<AuthenticatedResult> ChangePasswordAsync(ChangePasswordResource changePasswordResource, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ChangePassword requested for user: {Email}", changePasswordResource.Email);
        
        var result = await _accountRepository.ChangePassword(changePasswordResource);
        
        if (result.Success)
        {
            // Send email notification
            result = await SendPasswordChangeEmailAsync(result, changePasswordResource);
        }
        
        return result;
    }

    public async Task<AuthenticatedResult> ChangePasswordUserAsync(ChangePasswordResource changePasswordResource, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ChangePasswordUser requested for user: {Email}", changePasswordResource.Email);
        
        var result = await _accountRepository.ChangePasswordUser(changePasswordResource);
        
        if (result != null && result.Success)
        {
            // Send email notification
            result = await SendPasswordChangeEmailAsync(result, changePasswordResource);
        }
        
        return result;
    }

    public async Task<HttpResultResource> ChangeRoleUserAsync(ChangeRole changeRole, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ChangeRoleUser request received for user: {UserId}", changeRole.UserId);
        
        var result = await _accountRepository.ChangeRoleUser(changeRole);
        
        if (result != null && result.Success)
        {
            _logger.LogInformation("Change role successful for user: {UserId}", changeRole.UserId);
        }
        else
        {
            _logger.LogWarning("Change role failed for user: {UserId}", changeRole.UserId);
        }
        
        return result;
    }

    public async Task<HttpResultResource> DeleteAccountUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DeleteAccountUser request received for user: {UserId}", userId);
        
        var result = await _accountRepository.DeleteAccountUser(userId);
        
        if (result != null && result.Success)
        {
            _logger.LogInformation("Account deletion successful for user: {UserId}", userId);
        }
        else
        {
            _logger.LogWarning("Account deletion failed for user: {UserId}, Reason: {Reason}", 
                userId, result?.Messages ?? "Unknown");
        }
        
        return result;
    }

    public async Task<List<UserResource>> GetUserListAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetUserList request received");
        
        var users = await _accountRepository.GetUserList();
        
        _logger.LogInformation("Retrieved {Count} users", users.Count);
        return users;
    }

    private async Task<AuthenticatedResult> SendPasswordChangeEmailAsync(AuthenticatedResult result, ChangePasswordResource changePasswordResource)
    {
        const string MAILFAILURE = "Email Send Failure";
        const string TRUERESULT = "true";
        
        var message = changePasswordResource.Message
            .Replace("{appName}", changePasswordResource.AppName ?? "Klacks")
            .Replace("{password}", changePasswordResource.Password);
            
        var mailResult = await _accountRepository.SendEmail(
            changePasswordResource.Title, 
            changePasswordResource.Email, 
            message);

        result.MailSuccess = false;

        if (!string.IsNullOrEmpty(mailResult))
        {
            result.MailSuccess = string.Compare(mailResult, TRUERESULT) == 0;
        }

        if (!result.MailSuccess)
        {
            _accountRepository.SetModelError(result, MAILFAILURE, mailResult);
        }

        return result;
    }
}