// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Registers a user from the user administration. A rejected registration is reported with Identity's reason codes
/// (400, AccountRegistrationErrorCodes.ErrorCode), the same answer the own-admin setup gives, so the UI can name
/// the broken rule instead of a bare failure.
/// </summary>
/// <param name="accountRegistrationService">Creates the account and issues the first session</param>
/// <param name="authMapper">Maps the registration form onto the new AppUser</param>
/// <param name="unitOfWork">Wraps the registration in one transaction</param>

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Helpers;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

public class RegisterUserCommandHandler : BaseHandler, IRequestHandler<RegisterUserCommand, AuthenticatedResult>
{
    private readonly IAccountRegistrationService _accountRegistrationService;
    private readonly AuthMapper _authMapper;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUserCommandHandler(
        IAccountRegistrationService accountRegistrationService,
        AuthMapper authMapper,
        IUnitOfWork unitOfWork,
        ILogger<RegisterUserCommandHandler> logger)
        : base(logger)
    {
        _accountRegistrationService = accountRegistrationService;
        _authMapper = authMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthenticatedResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (!EmailAddressFormat.TryNormalize(request.Registration.Email, out var email))
        {
            throw AccountRegistrationRejectedException.InvalidEmail();
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            _logger.LogInformation("Processing user registration for: {Email}", email.MaskEmail());

            var userIdentity = _authMapper.ToAppUser(request.Registration);
            userIdentity.Email = email;
            var result = await _accountRegistrationService.RegisterUserAsync(userIdentity, request.Registration.Password ?? string.Empty);

            if (result.Success)
            {
                _logger.LogInformation("User registration successful: {Email}", email.MaskEmail());
                return result;
            }

            var rejection = ToRejection(result);
            _logger.LogWarning("User registration rejected: {Email}, Reasons: {Reasons}",
                email.MaskEmail(), string.Join(',', rejection.Reasons));
            throw rejection;
        });
    }

    private static AccountRegistrationRejectedException ToRejection(AuthenticatedResult result)
    {
        var modelState = result.ModelState;
        if (modelState == null)
        {
            return AccountRegistrationRejectedException.FromReasons([], [result.Message]);
        }

        return AccountRegistrationRejectedException.FromReasons(
            modelState.Keys,
            modelState.Values.SelectMany(entry => entry.Errors).Select(error => error.ErrorMessage));
    }
}
