// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.DTOs.Registrations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

public class ChangePasswordUserCommandHandler : BaseTransactionHandler, IRequestHandler<ChangePasswordUserCommand, AuthenticatedResult>
{
    private readonly IAccountPasswordService _accountPasswordService;
    private readonly IAccountNotificationService _accountNotificationService;
    private readonly IAccountAuthenticationService _accountAuthenticationService;
    
    public ChangePasswordUserCommandHandler(
        IAccountPasswordService accountPasswordService,
        IAccountNotificationService accountNotificationService,
        IAccountAuthenticationService accountAuthenticationService,
        IUnitOfWork unitOfWork,
        ILogger<ChangePasswordUserCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _accountPasswordService = accountPasswordService;
        _accountNotificationService = accountNotificationService;
        _accountAuthenticationService = accountAuthenticationService;
        }

    public async Task<AuthenticatedResult> Handle(ChangePasswordUserCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            var result = await _accountPasswordService.ChangePasswordAsync(request.ChangePassword);
            
            if (result != null && result.Success)
            {
                return await SendPasswordChangeEmailAsync(result, request.ChangePassword);
            }
            else
            {
                throw new InvalidRequestException("Password change failed.");
            }
        }, 
        "changing user password", 
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