// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Changes one role of an account inside a transaction. Before the Admin role is removed, the administrator-role lock is
/// taken inside that transaction, so two simultaneous removals are decided one after the other against the committed
/// administrator count (the count itself is enforced by the user management service).
/// </summary>
/// <param name="accountManagementService">Applies the role change</param>
/// <param name="administratorRoleLock">Serializes removals of the Admin role</param>

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.DTOs;
using Klacks.Api.Domain.DTOs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

public class ChangeRoleCommandHandler : BaseTransactionHandler, IRequestHandler<ChangeRoleCommand, HttpResultResource>
{
    private readonly IAccountManagementService _accountManagementService;
    private readonly IAdministratorRoleLock _administratorRoleLock;

    public ChangeRoleCommandHandler(
        IAccountManagementService accountManagementService,
        IAdministratorRoleLock administratorRoleLock,
        IUnitOfWork unitOfWork,
        ILogger<ChangeRoleCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _accountManagementService = accountManagementService;
        _administratorRoleLock = administratorRoleLock;
    }

    public async Task<HttpResultResource> Handle(ChangeRoleCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            if (!request.ChangeRole.IsSelected
                && string.Equals(request.ChangeRole.RoleName, Roles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                await _administratorRoleLock.AcquireAsync(cancellationToken);
            }

            var result = await _accountManagementService.ChangeRoleUserAsync(request.ChangeRole);
            
            if (result == null || !result.Success)
            {
                throw new ConflictException(result?.Messages ?? "Role change failed. The user might not exist or the role is invalid.");
            }
            
            return result;
        }, 
        "changing role", 
        new { UserId = request.ChangeRole.UserId });
    }
}