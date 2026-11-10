// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Queries.Accounts;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.DTOs.Registrations;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Application.DTOs.Registrations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

public class LoginUserQueryHandler : IRequestHandler<LoginUserQuery, TokenResource>
{
    private const string InvalidCredentialsMessage = "Invalid e-mail address or password.";

    private readonly IAccountAuthenticationService _accountAuthenticationService;
    private readonly IAdminSetupGateService _adminSetupGateService;
    private readonly ILogger<LoginUserQueryHandler> _logger;

    public LoginUserQueryHandler(
        IAccountAuthenticationService accountAuthenticationService,
        IAdminSetupGateService adminSetupGateService,
        ILogger<LoginUserQueryHandler> logger)
    {
        _accountAuthenticationService = accountAuthenticationService;
        _adminSetupGateService = adminSetupGateService;
        _logger = logger;
    }

    public async Task<TokenResource> Handle(LoginUserQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing login request for user: {Email}", request.Email.MaskEmail());

            if (await _adminSetupGateService.IsSeedAdminSignInReservedForSetupCodeAsync(request.Email, request.IsLocalRequest))
            {
                _logger.LogWarning("Remote sign-in with the seeded admin refused: the installation is claimed with its setup code");
                throw new UnauthorizedException(InvalidCredentialsMessage);
            }

            var result = await _accountAuthenticationService.LogInUserAsync(request.Email, request.Password);
            
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
                    Permissions = result.Permissions,
                    RefreshToken = result.RefreshToken,
                    Version = MyVersion.Get(),
                    Subject = request.Email
                };
                
                _logger.LogInformation("Login successful for user: {Email}", request.Email.MaskEmail());
                return response;
            }
            
            _logger.LogWarning("Login failed for user: {Email}", request.Email.MaskEmail());
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing login for user: {Email}", request.Email.MaskEmail());
            throw;
        }
    }
}