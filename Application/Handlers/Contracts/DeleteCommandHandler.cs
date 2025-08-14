using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Contracts;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<ContractResource>, ContractResource?>
{
    private readonly IContractRepository _contractRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCommandHandler> _logger;

    public DeleteCommandHandler(
        IContractRepository contractRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
    {
        _contractRepository = contractRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ContractResource?> Handle(DeleteCommand<ContractResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingContract = await _contractRepository.Get(request.Id);
            if (existingContract == null)
            {
                _logger.LogWarning("Contract with ID {ContractId} not found for deletion.", request.Id);
                return null;
            }

            var contractResource = _mapper.Map<ContractResource>(existingContract);
            await _contractRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return contractResource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting contract with ID {ContractId}.", request.Id);
            throw;
        }
    }
}