using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Domain.Models.Authentification;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Accounts;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthenticatedResult>
{
    private readonly AccountApplicationService _accountApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        AccountApplicationService accountApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _accountApplicationService = accountApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AuthenticatedResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            _logger.LogInformation("Processing user registration for: {Email}", request.Registration.Email);
            
            var result = await _accountApplicationService.RegisterUserAsync(request.Registration, cancellationToken);
            
            if (result != null && result.Success)
            {
                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync(transaction);
                _logger.LogInformation("User registration successful: {Email}", request.Registration.Email);
            }
            else
            {
                await _unitOfWork.RollbackTransactionAsync(transaction);
                _logger.LogWarning("User registration failed: {Email}", request.Registration.Email);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction);
            _logger.LogError(ex, "Error processing user registration: {Email}", request.Registration.Email);
            throw;
        }
    }
}