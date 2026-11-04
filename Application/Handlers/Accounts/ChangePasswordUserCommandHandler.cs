// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Helpers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

/// <summary>
/// Changes the caller's own password and mails a confirmation to the account's stored address. A failed attempt is
/// returned from the transaction instead of thrown inside it, so the failed-access count written for a wrong old
/// password is committed; the handler throws only after the transaction has finished. The confirmation mail is sent
/// only after the transaction has committed, so a retried or rolled-back transaction sends no mail.
/// </summary>
/// <param name="accountPasswordService">Performs the change, the lockout bookkeeping and the session renewal</param>
/// <param name="accountNotificationService">Sends the confirmation mail</param>
/// <param name="accountAuthenticationService">Records a mail failure on the result</param>
/// <param name="unitOfWork">Wraps the change in one transaction</param>
/// <param name="logger">Records the operation and any failure</param>
public class ChangePasswordUserCommandHandler : BaseTransactionHandler, IRequestHandler<ChangePasswordUserCommand, AuthenticatedResult>
{
    private const string OperationName = "changing user password";
    private const string PasswordChangeFailedMessage = "Password change failed.";

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
        var result = await ExecuteWithTransactionAsync(
            () => _accountPasswordService.ChangePasswordAsync(request.UserId, request.ChangePassword),
            OperationName,
            new { request.UserId });

        if (!result.Success)
        {
            throw new InvalidRequestException(PasswordChangeFailedMessage);
        }

        return await PasswordChangeEmailHelper.SendPasswordChangeEmailAsync(
            result, result.Email, request.ChangePassword, _accountNotificationService, _accountAuthenticationService);
    }
}
