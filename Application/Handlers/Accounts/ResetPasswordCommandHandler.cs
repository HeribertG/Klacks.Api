using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Services.Accounts;
using MediatR;

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
            return await _accountPasswordService.ResetPasswordAsync(request.ResetPassword);
        }, 
        "resetting password", 
        new { Token = request.ResetPassword.Token });
    }
}