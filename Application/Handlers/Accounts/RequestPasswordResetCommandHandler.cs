using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.DTOs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

public class RequestPasswordResetCommandHandler : BaseTransactionHandler, IRequestHandler<RequestPasswordResetCommand, HttpResultResource>
{
    private readonly IAccountPasswordService _accountPasswordService;
    
    public RequestPasswordResetCommandHandler(
        IAccountPasswordService accountPasswordService,
        IUnitOfWork unitOfWork,
        ILogger<RequestPasswordResetCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _accountPasswordService = accountPasswordService;
        }

    public async Task<HttpResultResource> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            var success = await _accountPasswordService.GeneratePasswordResetTokenAsync(request.Email);
            
            return new HttpResultResource
            {
                Success = true,
                Messages = "If this email address exists in our system, a password reset link has been sent."
            };
        }, 
        "requesting password reset", 
        new { Email = request.Email });
    }
}