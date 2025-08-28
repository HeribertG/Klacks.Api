using AutoMapper;
using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Services.Accounts;
using MediatR;

namespace Klacks.Api.Application.Handlers.Accounts;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthenticatedResult>
{
    private readonly IAccountRegistrationService _accountRegistrationService;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IAccountRegistrationService accountRegistrationService,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _accountRegistrationService = accountRegistrationService;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AuthenticatedResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            _logger.LogInformation("Processing user registration for: {Email}", request.Registration.Email);
            
            var userIdentity = _mapper.Map<AppUser>(request.Registration);
            var result = await _accountRegistrationService.RegisterUserAsync(userIdentity, request.Registration.Password);
            
            if (result != null && result.Success)
            {
                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync(transaction);
                _logger.LogInformation("User registration successful: {Email}", request.Registration.Email);
            }
            else
            {
                await _unitOfWork.RollbackTransactionAsync(transaction);
                _logger.LogWarning("User registration failed: {Email}, Reason: {Reason}", 
                    request.Registration.Email, result?.Message ?? "Unknown");
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