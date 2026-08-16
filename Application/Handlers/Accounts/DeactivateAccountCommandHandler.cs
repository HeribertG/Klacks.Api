// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
/// single-write account command, rather than the heavier deletion handler.
/// </summary>
/// <param name="accountManagementService">Performs the deactivation and reports its outcome.</param>
/// <param name="httpContextAccessor">Supplies the acting administrator's user id for the audit field.</param>
/// <param name="unitOfWork">Wraps the write in the transaction the other account commands use.</param>
/// <param name="logger">Records the operation and any failure.</param>
public class DeactivateAccountCommandHandler : BaseTransactionHandler, IRequestHandler<DeactivateAccountCommand, HttpResultResource>
{
    private const string UnknownActingUser = "Unknown";
    private const string OperationName = "deactivating account";

    private readonly IAccountManagementService _accountManagementService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeactivateAccountCommandHandler(
        IAccountManagementService accountManagementService,
        IHttpContextAccessor httpContextAccessor,
        IUnitOfWork unitOfWork,
        ILogger<DeactivateAccountCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _accountManagementService = accountManagementService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<HttpResultResource> Handle(DeactivateAccountCommand request, CancellationToken cancellationToken)
    {
        var deactivatedBy = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? UnknownActingUser;

        return await ExecuteWithTransactionAsync(async () =>
        {
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
