using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.DTOs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

public class UpdateAccountCommandHandler : BaseTransactionHandler, IRequestHandler<UpdateAccountCommand, HttpResultResource>
{
    private readonly IAccountManagementService _accountManagementService;

    public UpdateAccountCommandHandler(
        IAccountManagementService accountManagementService,
        IUnitOfWork unitOfWork,
        ILogger<UpdateAccountCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _accountManagementService = accountManagementService;
    }

    public async Task<HttpResultResource> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            var result = await _accountManagementService.UpdateAccountAsync(request.UpdateAccount);

            if (result == null || !result.Success)
            {
                throw new ConflictException(result?.Messages ?? "Account update failed.");
            }

            return result;
        },
        "updating account",
        new { UserId = request.UpdateAccount.Id });
    }
}
