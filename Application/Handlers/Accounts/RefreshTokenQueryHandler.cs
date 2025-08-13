using Klacks.Api.Application.Queries.Accounts;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Registrations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Accounts;

public class RefreshTokenQueryHandler : IRequestHandler<RefreshTokenQuery, TokenResource?>
{
    private readonly AccountApplicationService _accountApplicationService;
    private readonly ILogger<RefreshTokenQueryHandler> _logger;

    public RefreshTokenQueryHandler(
        AccountApplicationService accountApplicationService,
        ILogger<RefreshTokenQueryHandler> logger)
    {
        _accountApplicationService = accountApplicationService;
        _logger = logger;
    }

    public async Task<TokenResource?> Handle(RefreshTokenQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing token refresh request");
            
            var result = await _accountApplicationService.RefreshTokenAsync(request.RefreshRequest, cancellationToken);
            
            if (result == null)
            {
                _logger.LogWarning("Token refresh failed");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing token refresh");
            throw;
        }
    }
}