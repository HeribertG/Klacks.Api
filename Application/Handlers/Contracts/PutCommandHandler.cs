using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Contracts;

public class PutCommandHandler : IRequestHandler<PutCommand<ContractResource>, ContractResource?>
{
    private readonly IContractRepository _contractRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PutCommandHandler> _logger;

    public PutCommandHandler(
        IContractRepository contractRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
    {
        _contractRepository = contractRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ContractResource?> Handle(PutCommand<ContractResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingContract = await _contractRepository.Get(request.Resource.Id);
            if (existingContract == null)
            {
                _logger.LogWarning("Contract with ID {ContractId} not found.", request.Resource.Id);
                return null;
            }

            _mapper.Map(request.Resource, existingContract);
            await _contractRepository.Put(existingContract);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<ContractResource>(existingContract);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating contract with ID {ContractId}.", request.Resource.Id);
            throw;
        }
    }
}