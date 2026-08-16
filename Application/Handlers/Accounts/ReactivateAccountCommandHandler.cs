// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.DTOs;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Accounts;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

/// <summary>
/// Clears a deactivation so the account can sign in again.
/// </summary>
/// <param name="accountManagementService">Performs the reactivation and reports its outcome.</param>
/// <param name="unitOfWork">Wraps the write in the transaction the other account commands use.</param>
/// <param name="logger">Records the operation and any failure.</param>
public class ReactivateAccountCommandHandler : BaseTransactionHandler, IRequestHandler<ReactivateAccountCommand, HttpResultResource>
{
    private const string OperationName = "reactivating account";

    private readonly IAccountManagementService _accountManagementService;

    public ReactivateAccountCommandHandler(
        IAccountManagementService accountManagementService,
        IUnitOfWork unitOfWork,
        ILogger<ReactivateAccountCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _accountManagementService = accountManagementService;
    }

    public async Task<HttpResultResource> Handle(ReactivateAccountCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            var result = await _accountManagementService.ReactivateAccountUserAsync(request.UserId);

            if (result == null || !result.Success)
            {
                throw new ConflictException(result?.Messages ?? "Reactivation failed. The user might not exist.");
            }

            return result;
        },
        OperationName,
        new { request.UserId });
    }
}
