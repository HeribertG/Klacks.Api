using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Services.Accounts;
using MediatR;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Handlers.Accounts;

public class ResetPasswordCommandHandler : BaseTransactionHandler, IRequestHandler<ResetPasswordCommand, AuthenticatedResult>
{
    private readonly IAccountPasswordService _accountPasswordService;
    
    public ResetPasswordCommandHandler(
        IAccountPasswordService accountPasswordService,
        IUnitOfWork unitOfWork,
        ILogger<ResetPasswordCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _accountPasswordService = accountPasswordService;
        }

    public async Task<AuthenticatedResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            var result = await _accountPasswordService.ResetPasswordAsync(request.ResetPassword);

            if (result == null || !result.Success)
            {
                throw new InvalidRequestException(result?.Message ?? "Password reset failed.");
            }

            return result;
        }, 
        "resetting password", 
        new { Token = request.ResetPassword.Token });
    }
}