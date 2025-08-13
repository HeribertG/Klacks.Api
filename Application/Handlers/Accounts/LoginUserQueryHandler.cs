using Klacks.Api.Application.Queries.Accounts;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Registrations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Accounts;

public class LoginUserQueryHandler : IRequestHandler<LoginUserQuery, TokenResource?>
{
    private readonly AccountApplicationService _accountApplicationService;
    private readonly ILogger<LoginUserQueryHandler> _logger;

    public LoginUserQueryHandler(
        AccountApplicationService accountApplicationService,
        ILogger<LoginUserQueryHandler> logger)
    {
        _accountApplicationService = accountApplicationService;
        _logger = logger;
    }

    public async Task<TokenResource?> Handle(LoginUserQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing login request for user: {Email}", request.Email);
            
            var result = await _accountApplicationService.LoginUserAsync(request.Email, request.Password, cancellationToken);
            
            if (result == null)
            {
                _logger.LogWarning("Login failed for user: {Email}", request.Email);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing login for user: {Email}", request.Email);
            throw;
        }
    }
}