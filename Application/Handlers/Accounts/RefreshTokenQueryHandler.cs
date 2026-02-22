// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Queries.Accounts;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.DTOs.Registrations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

public class RefreshTokenQueryHandler : IRequestHandler<RefreshTokenQuery, TokenResource?>
{
    private readonly IAccountAuthenticationService _accountAuthenticationService;
    private readonly ILogger<RefreshTokenQueryHandler> _logger;

    public RefreshTokenQueryHandler(
        IAccountAuthenticationService accountAuthenticationService,
        ILogger<RefreshTokenQueryHandler> logger)
    {
        _accountAuthenticationService = accountAuthenticationService;
        _logger = logger;
    }

    public async Task<TokenResource?> Handle(RefreshTokenQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing token refresh request");
            
            var result = await _accountAuthenticationService.RefreshTokenAsync(request.RefreshRequest);
            
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
            
            _logger.LogWarning("Token refresh failed - returning null");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing token refresh");
            throw;
        }
    }
}