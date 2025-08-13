using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Contracts;

public class PutCommandHandler : IRequestHandler<PutCommand<ContractResource>, ContractResource?>
{
    private readonly ContractApplicationService _contractApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PutCommandHandler> _logger;

    public PutCommandHandler(
        ContractApplicationService contractApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
    {
        _contractApplicationService = contractApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ContractResource?> Handle(PutCommand<ContractResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingContract = await _contractApplicationService.GetContractByIdAsync(request.Resource.Id, cancellationToken);
            if (existingContract == null)
            {
                _logger.LogWarning("Contract with ID {ContractId} not found.", request.Resource.Id);
                return null;
            }

            var result = await _contractApplicationService.UpdateContractAsync(request.Resource, cancellationToken);
            await _unitOfWork.CompleteAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating contract with ID {ContractId}.", request.Resource.Id);
            throw;
        }
    }
}