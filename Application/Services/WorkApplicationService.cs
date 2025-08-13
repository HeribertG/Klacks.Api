using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services;

public class WorkApplicationService
{
    private readonly IWorkRepository _workRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<WorkApplicationService> _logger;

    public WorkApplicationService(
        IWorkRepository workRepository,
        IClientRepository clientRepository,
        IMapper mapper,
        ILogger<WorkApplicationService> logger)
    {
        _workRepository = workRepository;
        _clientRepository = clientRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<WorkResource?> GetWorkByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var work = await _workRepository.Get(id);
        return work != null ? _mapper.Map<WorkResource>(work) : null;
    }

    public async Task<List<ClientWorkResource>> GetAllWorksAsync(WorkFilter? filter, CancellationToken cancellationToken = default)
    {
        var clients = await _clientRepository.WorkList(filter);
        return _mapper.Map<List<ClientWorkResource>>(clients);
    }

    public async Task<WorkResource> CreateWorkAsync(WorkResource workResource, CancellationToken cancellationToken = default)
    {
        var work = _mapper.Map<Work>(workResource);
        await _workRepository.Add(work);
        return _mapper.Map<WorkResource>(work);
    }

    public async Task<WorkResource> UpdateWorkAsync(WorkResource workResource, CancellationToken cancellationToken = default)
    {
        var work = _mapper.Map<Work>(workResource);
        var updatedWork = await _workRepository.Put(work);
        return _mapper.Map<WorkResource>(updatedWork);
    }
}