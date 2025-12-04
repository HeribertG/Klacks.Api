using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Contracts;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<ContractResource>, ContractResource?>
{
    private readonly IContractRepository _contractRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IContractRepository contractRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _contractRepository = contractRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ContractResource?> Handle(PutCommand<ContractResource> request, CancellationToken cancellationToken)
    {
        ValidateContractRequest(request.Resource);

        return await ExecuteAsync(async () =>
        {
            var existingContract = await _contractRepository.Get(request.Resource.Id);
            if (existingContract == null)
            {
                throw new KeyNotFoundException($"Contract with ID {request.Resource.Id} not found.");
            }

            _mapper.Map(request.Resource, existingContract);
            await _contractRepository.Put(existingContract);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<ContractResource>(existingContract);
        }, 
        "updating contract", 
        new { ContractId = request.Resource.Id, ContractName = request.Resource?.Name });
    }

    private void ValidateContractRequest(ContractResource? resource)
    {
        if (resource == null)
        {
            throw new InvalidRequestException("Contract data is required.");
        }

        if (string.IsNullOrWhiteSpace(resource.Name))
        {
            throw new InvalidRequestException("Contract name is required.");
        }

        if (resource.MinimumHoursPerMonth > resource.MaximumHoursPerMonth)
        {
            throw new InvalidRequestException("Minimum hours cannot exceed maximum hours.");
        }

        if (resource.GuaranteedHoursPerMonth < resource.MinimumHoursPerMonth ||
            resource.GuaranteedHoursPerMonth > resource.MaximumHoursPerMonth)
        {
            throw new InvalidRequestException("Guaranteed hours must be between minimum and maximum hours.");
        }

        if (resource.ValidUntil.HasValue && resource.ValidUntil <= resource.ValidFrom)
        {
            throw new InvalidRequestException("Valid until date must be after valid from date.");
        }
    }
}