using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Accounts;

public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, HttpResultResource>
{
    private readonly AccountApplicationService _accountApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteAccountCommandHandler> _logger;

    public DeleteAccountCommandHandler(
        AccountApplicationService accountApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteAccountCommandHandler> logger)
    {
        _accountApplicationService = accountApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<HttpResultResource> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            _logger.LogInformation("Processing account deletion for user: {UserId}", request.UserId);
            
            var result = await _accountApplicationService.DeleteAccountUserAsync(request.UserId, cancellationToken);
            
            if (result != null && result.Success)
            {
                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync(transaction);
                _logger.LogInformation("Account deletion successful for user: {UserId}", request.UserId);
            }
            else
            {
                await _unitOfWork.RollbackTransactionAsync(transaction);
                _logger.LogWarning("Account deletion failed for user: {UserId}", request.UserId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction);
            _logger.LogError(ex, "Error deleting account for user: {UserId}", request.UserId);
            throw;
        }
    }
}