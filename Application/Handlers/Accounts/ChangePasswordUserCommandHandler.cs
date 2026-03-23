// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Helpers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Interfaces;
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
                return await PasswordChangeEmailHelper.SendPasswordChangeEmailAsync(
                    result, request.ChangePassword, _accountNotificationService, _accountAuthenticationService);
            }
            else
            {
                throw new InvalidRequestException("Password change failed.");
            }
        },
        "changing user password",
        new { Email = request.ChangePassword.Email });
    }
}