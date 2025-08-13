using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Contracts;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<ContractResource>, ContractResource?>
{
    private readonly ContractApplicationService _contractApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCommandHandler> _logger;

    public DeleteCommandHandler(
        ContractApplicationService contractApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
    {
        _contractApplicationService = contractApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ContractResource?> Handle(DeleteCommand<ContractResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingContract = await _contractApplicationService.GetContractByIdAsync(request.Id, cancellationToken);
            if (existingContract == null)
            {
                _logger.LogWarning("Contract with ID {ContractId} not found for deletion.", request.Id);
                return null;
            }

            await _contractApplicationService.DeleteContractAsync(request.Id, cancellationToken);
            await _unitOfWork.CompleteAsync();

            return existingContract;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting contract with ID {ContractId}.", request.Id);
            throw;
        }
    }
}