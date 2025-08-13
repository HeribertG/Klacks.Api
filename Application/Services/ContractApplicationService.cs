using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Presentation.DTOs.Associations;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services;

public class ContractApplicationService
{
    private readonly IContractRepository _contractRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ContractApplicationService> _logger;

    public ContractApplicationService(
        IContractRepository contractRepository,
        IMapper mapper,
        ILogger<ContractApplicationService> logger)
    {
        _contractRepository = contractRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ContractResource?> GetContractByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contract = await _contractRepository.Get(id);
        return contract != null ? _mapper.Map<ContractResource>(contract) : null;
    }

    public async Task<List<ContractResource>> GetAllContractsAsync(CancellationToken cancellationToken = default)
    {
        var contracts = await _contractRepository.List();
        return _mapper.Map<List<ContractResource>>(contracts);
    }

    public async Task<ContractResource> CreateContractAsync(ContractResource contractResource, CancellationToken cancellationToken = default)
    {
        var contract = _mapper.Map<Contract>(contractResource);
        await _contractRepository.Add(contract);
        return _mapper.Map<ContractResource>(contract);
    }

    public async Task<ContractResource> UpdateContractAsync(ContractResource contractResource, CancellationToken cancellationToken = default)
    {
        var contract = _mapper.Map<Contract>(contractResource);
        var updatedContract = await _contractRepository.Put(contract);
        return _mapper.Map<ContractResource>(updatedContract);
    }

    public async Task DeleteContractAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _contractRepository.Delete(id);
    }
}