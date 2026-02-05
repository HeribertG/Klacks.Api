using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.DTOs;
using Klacks.Api.Infrastructure.Mediator;
using System.Collections.Generic;

namespace Klacks.Api.Application.Handlers.Accounts;

public class DeleteAccountCommandHandler : BaseHandler, IRequestHandler<DeleteAccountCommand, HttpResultResource>
{
    private readonly IAccountManagementService _accountManagementService;
    private readonly IUnitOfWork _unitOfWork;
    
    public DeleteAccountCommandHandler(
        IAccountManagementService accountManagementService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteAccountCommandHandler> logger)
        : base(logger)
    {
        _accountManagementService = accountManagementService;
        _unitOfWork = unitOfWork;
        }

    public async Task<HttpResultResource> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            _logger.LogInformation("Processing account deletion for user: {UserId}", request.UserId);
            
            var result = await _accountManagementService.DeleteAccountUserAsync(request.UserId);

            if (result == null || !result.Success)
            {
                throw new KeyNotFoundException($"User with ID {request.UserId} not found or could not be deleted.");
            }

            await _unitOfWork.CompleteAsync();
            await _unitOfWork.CommitTransactionAsync(transaction);
            _logger.LogInformation("Account deletion successful for user: {UserId}", request.UserId);

            return result;
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction);
            _logger.LogError("Error deleting account for user: {UserId}", request.UserId);
            throw;
        }
    }
}