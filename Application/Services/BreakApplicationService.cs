using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services;

public class BreakApplicationService
{
    private readonly IBreakRepository _breakRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<BreakApplicationService> _logger;

    public BreakApplicationService(
        IBreakRepository breakRepository,
        IMapper mapper,
        ILogger<BreakApplicationService> logger)
    {
        _breakRepository = breakRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BreakResource?> GetBreakByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var breakItem = await _breakRepository.Get(id);
        return breakItem != null ? _mapper.Map<BreakResource>(breakItem) : null;
    }

    public async Task<List<BreakResource>> GetAllBreaksAsync(CancellationToken cancellationToken = default)
    {
        var breaks = await _breakRepository.List();
        return _mapper.Map<List<BreakResource>>(breaks);
    }

    public async Task<BreakResource> CreateBreakAsync(BreakResource breakResource, CancellationToken cancellationToken = default)
    {
        var breakItem = _mapper.Map<Break>(breakResource);
        await _breakRepository.Add(breakItem);
        return _mapper.Map<BreakResource>(breakItem);
    }

    public async Task<BreakResource> UpdateBreakAsync(BreakResource breakResource, CancellationToken cancellationToken = default)
    {
        var breakItem = _mapper.Map<Break>(breakResource);
        var updatedBreak = await _breakRepository.Put(breakItem);
        return _mapper.Map<BreakResource>(updatedBreak);
    }

    public async Task DeleteBreakAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _breakRepository.Delete(id);
    }
}