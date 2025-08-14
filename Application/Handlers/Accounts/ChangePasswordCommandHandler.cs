using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Services.Accounts;
using Klacks.Api.Presentation.DTOs.Registrations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Accounts;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, AuthenticatedResult>
{
    private readonly IAccountPasswordService _accountPasswordService;
    private readonly IAccountNotificationService _accountNotificationService;
    private readonly IAccountAuthenticationService _accountAuthenticationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IAccountPasswordService accountPasswordService,
        IAccountNotificationService accountNotificationService,
        IAccountAuthenticationService accountAuthenticationService,
        IUnitOfWork unitOfWork,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _accountPasswordService = accountPasswordService;
        _accountNotificationService = accountNotificationService;
        _accountAuthenticationService = accountAuthenticationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AuthenticatedResult> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            _logger.LogInformation("Processing password change for user: {Email}", request.ChangePassword.Email);
            
            var result = await _accountPasswordService.ChangePasswordAsync(request.ChangePassword);
            
            if (result.Success)
            {
                // Send email notification
                result = await SendPasswordChangeEmailAsync(result, request.ChangePassword);
                
                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync(transaction);
                _logger.LogInformation("Password change successful for user: {Email}", request.ChangePassword.Email);
            }
            else
            {
                await _unitOfWork.RollbackTransactionAsync(transaction);
                _logger.LogWarning("Password change failed for user: {Email}", request.ChangePassword.Email);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction);
            _logger.LogError(ex, "Error changing password for user: {Email}", request.ChangePassword.Email);
            throw;
        }
    }
    
    private async Task<AuthenticatedResult> SendPasswordChangeEmailAsync(AuthenticatedResult result, ChangePasswordResource changePasswordResource)
    {
        const string MAILFAILURE = "Email Send Failure";
        const string TRUERESULT = "true";
        
        var message = changePasswordResource.Message
            .Replace("{appName}", changePasswordResource.AppName ?? "Klacks")
            .Replace("{password}", changePasswordResource.Password);
            
        var mailResult = await _accountNotificationService.SendEmailAsync(
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
            _accountAuthenticationService.SetModelErrorAsync(result, MAILFAILURE, mailResult);
        }

        return result;
    }
}