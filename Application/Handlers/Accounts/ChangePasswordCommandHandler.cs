// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Helpers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Exceptions;
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
                return await PasswordChangeEmailHelper.SendPasswordChangeEmailAsync(
                    passwordResult, request.ChangePassword, _accountNotificationService, _accountAuthenticationService);
            }
            else
            {
                throw new InvalidRequestException("Password change failed.");
            }
        },
        "changing password",
        new { Email = request.ChangePassword.Email });
    }
}