using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Settings;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services;

public class CommunicationApplicationService
{
    private readonly ICommunicationRepository _communicationRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CommunicationApplicationService> _logger;

    public CommunicationApplicationService(
        ICommunicationRepository communicationRepository,
        IMapper mapper,
        ILogger<CommunicationApplicationService> logger)
    {
        _communicationRepository = communicationRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<CommunicationResource?> GetCommunicationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var communication = await _communicationRepository.Get(id);
        return communication != null ? _mapper.Map<CommunicationResource>(communication) : null;
    }

    public async Task<List<CommunicationResource>> GetAllCommunicationsAsync(CancellationToken cancellationToken = default)
    {
        var communications = await _communicationRepository.List();
        return _mapper.Map<List<CommunicationResource>>(communications);
    }

    public async Task<List<CommunicationResource>> GetClientCommunicationsAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var communications = await _communicationRepository.GetClient(clientId);
        return _mapper.Map<List<CommunicationResource>>(communications);
    }

    public async Task<List<CommunicationTypeResource>> GetCommunicationTypesAsync(CancellationToken cancellationToken = default)
    {
        var types = await _communicationRepository.TypeList();
        return _mapper.Map<List<CommunicationTypeResource>>(types);
    }

    public async Task<CommunicationResource> CreateCommunicationAsync(CommunicationResource communicationResource, CancellationToken cancellationToken = default)
    {
        var communication = _mapper.Map<Communication>(communicationResource);
        await _communicationRepository.Add(communication);
        return _mapper.Map<CommunicationResource>(communication);
    }

    public async Task<CommunicationResource> UpdateCommunicationAsync(CommunicationResource communicationResource, CancellationToken cancellationToken = default)
    {
        var communication = _mapper.Map<Communication>(communicationResource);
        var updatedCommunication = await _communicationRepository.Put(communication);
        return _mapper.Map<CommunicationResource>(updatedCommunication);
    }

    public async Task DeleteCommunicationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _communicationRepository.Delete(id);
    }
}