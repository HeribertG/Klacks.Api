using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Queries.Accounts;
using Klacks.Api.Domain.Services.Accounts;
using Klacks.Api.Presentation.DTOs.Registrations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Accounts;

public class LoginUserQueryHandler : IRequestHandler<LoginUserQuery, TokenResource>
{
    private readonly IAccountAuthenticationService _accountAuthenticationService;
    private readonly ILogger<LoginUserQueryHandler> _logger;

    public LoginUserQueryHandler(
        IAccountAuthenticationService accountAuthenticationService,
        ILogger<LoginUserQueryHandler> logger)
    {
        _accountAuthenticationService = accountAuthenticationService;
        _logger = logger;
    }

    public async Task<TokenResource> Handle(LoginUserQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing login request for user: {Email}", request.Email);
            
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
                    RefreshToken = result.RefreshToken,
                    Version = new MyVersion().Get(),
                    Subject = request.Email
                };
                
                _logger.LogInformation("Login successful for user: {Email}", request.Email);
                return response;
            }
            
            _logger.LogWarning("Login failed for user: {Email}", request.Email);
            throw new UnauthorizedException("Invalid e-mail address or password.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing login for user: {Email}", request.Email);
            throw;
        }
    }
}