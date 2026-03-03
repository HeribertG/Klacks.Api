// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Queries.Accounts;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

public class ValidatePasswordResetTokenQueryHandler : IRequestHandler<ValidatePasswordResetTokenQuery, bool>
{
    private readonly IAccountPasswordService _accountPasswordService;
    private readonly ILogger<ValidatePasswordResetTokenQueryHandler> _logger;

    public ValidatePasswordResetTokenQueryHandler(
        IAccountPasswordService accountPasswordService,
        ILogger<ValidatePasswordResetTokenQueryHandler> logger)
    {
        _accountPasswordService = accountPasswordService;
        _logger = logger;
    }

    public async Task<bool> Handle(ValidatePasswordResetTokenQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Validating password reset token");
            
            var isValid = await _accountPasswordService.ValidatePasswordResetTokenAsync(request.Token);
            
            _logger.LogInformation("Token validation result: {IsValid}", isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating password reset token");
            return false;
        }
    }
}