using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Presentation.DTOs.Settings;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services;

public class StateApplicationService
{
    private readonly IStateRepository _stateRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<StateApplicationService> _logger;

    public StateApplicationService(
        IStateRepository stateRepository,
        IMapper mapper,
        ILogger<StateApplicationService> logger)
    {
        _stateRepository = stateRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<StateResource?> GetStateByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var state = await _stateRepository.Get(id);
        return state != null ? _mapper.Map<StateResource>(state) : null;
    }

    public async Task<List<StateResource>> GetAllStatesAsync(CancellationToken cancellationToken = default)
    {
        var states = await _stateRepository.List();
        return _mapper.Map<List<StateResource>>(states);
    }

    public async Task<StateResource> CreateStateAsync(StateResource stateResource, CancellationToken cancellationToken = default)
    {
        var state = _mapper.Map<State>(stateResource);
        await _stateRepository.Add(state);
        return _mapper.Map<StateResource>(state);
    }

    public async Task<StateResource> UpdateStateAsync(StateResource stateResource, CancellationToken cancellationToken = default)
    {
        var state = _mapper.Map<State>(stateResource);
        var updatedState = await _stateRepository.Put(state);
        return _mapper.Map<StateResource>(updatedState);
    }

    public async Task DeleteStateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _stateRepository.Delete(id);
    }
}