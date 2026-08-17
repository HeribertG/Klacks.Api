// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Applies an admin's drag'n'drop reorder of the user administration list.
/// </summary>
/// <param name="accountManagementService">Owns the AppUser.DisplayOrder write.</param>

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Domain.DTOs;
using Klacks.Api.Domain.Interfaces.Accounts;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

public class ReorderUsersCommandHandler : IRequestHandler<ReorderUsersCommand, HttpResultResource>
{
    private readonly IAccountManagementService _accountManagementService;

    public ReorderUsersCommandHandler(IAccountManagementService accountManagementService)
    {
        _accountManagementService = accountManagementService;
    }

    public async Task<HttpResultResource> Handle(ReorderUsersCommand request, CancellationToken cancellationToken)
    {
        return await _accountManagementService.ReorderUsersAsync(request.OrderedUserIds);
    }
}
