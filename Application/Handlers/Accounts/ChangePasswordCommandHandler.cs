using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Registrations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

public class ChangePasswordCommandHandler : BaseTransactionHandler, IRequestHandler<ChangePasswordCommand, AuthenticatedResult>
{
    private readonly IAccountPasswordService _accountPasswordService;
    private readonly IAccountNotificationService _accountNotificationService;
    private readonly IAccountAuthenticationService _accountAuthenticationService;
    
    public ChangePasswordCommandHandler(
        IAccountPasswordService accountPasswordService,
        IAccountNotificationService accountNotificationService,
        IAccountAuthenticationService accountAuthenticationService,
        IUnitOfWork unitOfWork,
        ILogger<ChangePasswordCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _accountPasswordService = accountPasswordService;
        _accountNotificationService = accountNotificationService;
        _accountAuthenticationService = accountAuthenticationService;
    }

    public async Task<AuthenticatedResult> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            var passwordResult = await _accountPasswordService.ChangePasswordAsync(request.ChangePassword);
            
            if (passwordResult.Success)
            {
                return await SendPasswordChangeEmailAsync(passwordResult, request.ChangePassword);
            }
            else
            {
                throw new InvalidRequestException("Password change failed.");
            }
        }, 
        "changing password", 
        new { Email = request.ChangePassword.Email });
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