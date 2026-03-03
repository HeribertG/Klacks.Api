// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.DTOs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

public class ChangeRoleCommandHandler : BaseTransactionHandler, IRequestHandler<ChangeRoleCommand, HttpResultResource>
{
    private readonly IAccountManagementService _accountManagementService;
    
    public ChangeRoleCommandHandler(
        IAccountManagementService accountManagementService,
        IUnitOfWork unitOfWork,
        ILogger<ChangeRoleCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _accountManagementService = accountManagementService;
        }

    public async Task<HttpResultResource> Handle(ChangeRoleCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
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