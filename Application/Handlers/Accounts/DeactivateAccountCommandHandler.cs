// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Security.Claims;
using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.DTOs;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Accounts;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

/// <summary>
/// Deactivates an account and records who did it. Mirrors ChangeRoleCommandHandler, the other
/// single-write account command, rather than the heavier deletion handler. The administrator-role lock is taken inside
/// the transaction before the service reads the administrator count, so a deactivation and a simultaneous role removal
/// or deactivation are decided one after the other and the last active administrator stays.
/// </summary>
/// <param name="accountManagementService">Performs the deactivation and reports its outcome.</param>
/// <param name="administratorRoleLock">Serializes deactivations and removals of the Admin role.</param>
/// <param name="httpContextAccessor">Supplies the acting administrator's user id for the audit field.</param>
/// <param name="unitOfWork">Wraps the write in the transaction the other account commands use.</param>
/// <param name="logger">Records the operation and any failure.</param>
public class DeactivateAccountCommandHandler : BaseTransactionHandler, IRequestHandler<DeactivateAccountCommand, HttpResultResource>
{
    private const string UnknownActingUser = "Unknown";
    private const string OperationName = "deactivating account";

    private readonly IAccountManagementService _accountManagementService;
    private readonly IAdministratorRoleLock _administratorRoleLock;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeactivateAccountCommandHandler(
        IAccountManagementService accountManagementService,
        IAdministratorRoleLock administratorRoleLock,
        IHttpContextAccessor httpContextAccessor,
        IUnitOfWork unitOfWork,
        ILogger<DeactivateAccountCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _accountManagementService = accountManagementService;
        _administratorRoleLock = administratorRoleLock;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<HttpResultResource> Handle(DeactivateAccountCommand request, CancellationToken cancellationToken)
    {
        var deactivatedBy = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? UnknownActingUser;

        return await ExecuteWithTransactionAsync(async () =>
        {
            await _administratorRoleLock.AcquireAsync(cancellationToken);

            var result = await _accountManagementService.DeactivateAccountUserAsync(request.UserId, deactivatedBy);

            if (result == null || !result.Success)
            {
                throw new ConflictException(result?.Messages ?? "Deactivation failed. The user might not exist.");
            }

            return result;
        },
        OperationName,
        new { request.UserId });
    }
}
