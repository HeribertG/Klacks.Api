using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Domain.Models.Authentification;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Accounts;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, AuthenticatedResult>
{
    private readonly AccountApplicationService _accountApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        AccountApplicationService accountApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _accountApplicationService = accountApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AuthenticatedResult> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            _logger.LogInformation("Processing password change for user: {Email}", request.ChangePassword.Email);
            
            var result = await _accountApplicationService.ChangePasswordAsync(request.ChangePassword, cancellationToken);
            
            if (result.Success)
            {
                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync(transaction);
                _logger.LogInformation("Password change successful for user: {Email}", request.ChangePassword.Email);
            }
            else
            {
                await _unitOfWork.RollbackTransactionAsync(transaction);
                _logger.LogWarning("Password change failed for user: {Email}", request.ChangePassword.Email);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction);
            _logger.LogError(ex, "Error changing password for user: {Email}", request.ChangePassword.Email);
            throw;
        }
    }
}