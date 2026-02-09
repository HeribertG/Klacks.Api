using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

public class RegisterUserCommandHandler : BaseHandler, IRequestHandler<RegisterUserCommand, AuthenticatedResult>
{
    private readonly IAccountRegistrationService _accountRegistrationService;
    private readonly AuthMapper _authMapper;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUserCommandHandler(
        IAccountRegistrationService accountRegistrationService,
        AuthMapper authMapper,
        IUnitOfWork unitOfWork,
        ILogger<RegisterUserCommandHandler> logger)
        : base(logger)
    {
        _accountRegistrationService = accountRegistrationService;
        _authMapper = authMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<AuthenticatedResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        var transactionCompleted = false;

        try
        {
            _logger.LogInformation("Processing user registration for: {Email}", request.Registration.Email);

            var userIdentity = _authMapper.ToAppUser(request.Registration);
            var result = await _accountRegistrationService.RegisterUserAsync(userIdentity, request.Registration.Password);

            if (result != null && result.Success)
            {
                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync(transaction);
                transactionCompleted = true;
                _logger.LogInformation("User registration successful: {Email}", request.Registration.Email);
                return result;
            }

            await _unitOfWork.RollbackTransactionAsync(transaction);
            transactionCompleted = true;
            _logger.LogWarning("User registration failed: {Email}, Reason: {Reason}",
                request.Registration.Email, result?.Message ?? "Unknown");
            throw new ConflictException(result?.Message ?? "User registration failed.");
        }
        catch (Exception ex) when (!transactionCompleted)
        {
            try
            {
                await _unitOfWork.RollbackTransactionAsync(transaction);
            }
            catch (InvalidOperationException)
            {
                _logger.LogWarning("Transaction already completed during error handling for: {Email}", request.Registration.Email);
            }
            _logger.LogError(ex, "Error processing user registration: {Email}", request.Registration.Email);
            throw;
        }
    }
}