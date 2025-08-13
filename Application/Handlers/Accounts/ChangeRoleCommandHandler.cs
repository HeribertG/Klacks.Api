using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Accounts;

public class ChangeRoleCommandHandler : IRequestHandler<ChangeRoleCommand, HttpResultResource>
{
    private readonly AccountApplicationService _accountApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChangeRoleCommandHandler> _logger;

    public ChangeRoleCommandHandler(
        AccountApplicationService accountApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<ChangeRoleCommandHandler> logger)
    {
        _accountApplicationService = accountApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<HttpResultResource> Handle(ChangeRoleCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            _logger.LogInformation("Processing role change for user: {UserId}", request.ChangeRole.UserId);
            
            var result = await _accountApplicationService.ChangeRoleUserAsync(request.ChangeRole, cancellationToken);
            
            if (result != null && result.Success)
            {
                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync(transaction);
                _logger.LogInformation("Role change successful for user: {UserId}", request.ChangeRole.UserId);
            }
            else
            {
                await _unitOfWork.RollbackTransactionAsync(transaction);
                _logger.LogWarning("Role change failed for user: {UserId}", request.ChangeRole.UserId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction);
            _logger.LogError(ex, "Error changing role for user: {UserId}", request.ChangeRole.UserId);
            throw;
        }
    }
}