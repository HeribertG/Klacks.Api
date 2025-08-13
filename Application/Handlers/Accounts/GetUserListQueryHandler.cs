using Klacks.Api.Application.Queries.Accounts;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Registrations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Accounts;

public class GetUserListQueryHandler : IRequestHandler<GetUserListQuery, List<UserResource>>
{
    private readonly AccountApplicationService _accountApplicationService;
    private readonly ILogger<GetUserListQueryHandler> _logger;

    public GetUserListQueryHandler(
        AccountApplicationService accountApplicationService,
        ILogger<GetUserListQueryHandler> logger)
    {
        _accountApplicationService = accountApplicationService;
        _logger = logger;
    }

    public async Task<List<UserResource>> Handle(GetUserListQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing get user list request");
            
            var result = await _accountApplicationService.GetUserListAsync(cancellationToken);
            
            _logger.LogInformation("Retrieved {Count} users", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user list");
            throw;
        }
    }
}