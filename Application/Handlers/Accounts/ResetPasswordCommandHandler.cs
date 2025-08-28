using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Services.Accounts;
using MediatR;

namespace Klacks.Api.Application.Handlers.Accounts;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, AuthenticatedResult>
{
    private readonly IAccountPasswordService _accountPasswordService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IAccountPasswordService accountPasswordService,
        IUnitOfWork unitOfWork,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _accountPasswordService = accountPasswordService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AuthenticatedResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            _logger.LogInformation("Processing password reset with token");
            
            var result = await _accountPasswordService.ResetPasswordAsync(request.ResetPassword);
            
            if (result.Success)
            {
                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync(transaction);
                _logger.LogInformation("Password reset successful");
            }
            else
            {
                await _unitOfWork.RollbackTransactionAsync(transaction);
                _logger.LogWarning("Password reset failed");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction);
            _logger.LogError(ex, "Error resetting password");
            throw;
        }
    }
}