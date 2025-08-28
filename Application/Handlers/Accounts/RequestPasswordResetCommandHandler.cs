using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Services.Accounts;
using Klacks.Api.Presentation.DTOs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Accounts;

public class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, HttpResultResource>
{
    private readonly IAccountPasswordService _accountPasswordService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RequestPasswordResetCommandHandler> _logger;

    public RequestPasswordResetCommandHandler(
        IAccountPasswordService accountPasswordService,
        IUnitOfWork unitOfWork,
        ILogger<RequestPasswordResetCommandHandler> logger)
    {
        _accountPasswordService = accountPasswordService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<HttpResultResource> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            _logger.LogInformation("Processing password reset request for email: {Email}", request.Email);
            
            var success = await _accountPasswordService.GeneratePasswordResetTokenAsync(request.Email);
            
            if (success)
            {
                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync(transaction);
                _logger.LogInformation("Password reset email sent successfully to: {Email}", request.Email);
                
                return new HttpResultResource
                {
                    Success = true,
                    Messages = "If this email address exists in our system, a password reset link has been sent."
                };
            }
            else
            {
                await _unitOfWork.RollbackTransactionAsync(transaction);
                _logger.LogWarning("Password reset request failed for email: {Email}", request.Email);
                
                // For security reasons, we always return the same message
                return new HttpResultResource
                {
                    Success = true,
                    Messages = "If this email address exists in our system, a password reset link has been sent."
                };
            }
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction);
            _logger.LogError(ex, "Error processing password reset request for email: {Email}", request.Email);
            
            return new HttpResultResource
            {
                Success = false,
                Messages = "An error occurred. Please try again later."
            };
        }
    }
}