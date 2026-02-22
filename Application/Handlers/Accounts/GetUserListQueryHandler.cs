// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Queries.Accounts;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.DTOs.Registrations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

public class GetUserListQueryHandler : IRequestHandler<GetUserListQuery, List<UserResource>>
{
    private readonly IAccountManagementService _accountManagementService;
    private readonly ILogger<GetUserListQueryHandler> _logger;

    public GetUserListQueryHandler(
        IAccountManagementService accountManagementService,
        ILogger<GetUserListQueryHandler> logger)
    {
        _accountManagementService = accountManagementService;
        _logger = logger;
    }

    public async Task<List<UserResource>> Handle(GetUserListQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing get user list request");
            
            var result = await _accountManagementService.GetUserListAsync();
            
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