using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services;

public class ShiftApplicationService : IShiftApplicationService
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ShiftApplicationService> _logger;

    public ShiftApplicationService(
        IShiftRepository shiftRepository,
        IMapper mapper,
        ILogger<ShiftApplicationService> logger)
    {
        _shiftRepository = shiftRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ShiftResource?> GetShiftByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var shift = await _shiftRepository.Get(id);
        return shift != null ? _mapper.Map<ShiftResource>(shift) : null;
    }

    public async Task<TruncatedShiftResource> GetTruncatedShiftsAsync(ShiftFilter filter, CancellationToken cancellationToken = default)
    {
        var truncated = await _shiftRepository.GetFilteredAndPaginatedShifts(filter);
        return _mapper.Map<TruncatedShiftResource>(truncated);
    }

    public async Task<List<ShiftResource>> GetShiftCutsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cuts = await _shiftRepository.CutList(id);
        return _mapper.Map<List<ShiftResource>>(cuts);
    }

    public async Task<ShiftResource> CreateShiftAsync(ShiftResource shiftResource, CancellationToken cancellationToken = default)
    {
        var shift = _mapper.Map<Shift>(shiftResource);
        await _shiftRepository.Add(shift);
        return _mapper.Map<ShiftResource>(shift);
    }

    public async Task<ShiftResource> UpdateShiftAsync(ShiftResource shiftResource, CancellationToken cancellationToken = default)
    {
        var shift = _mapper.Map<Shift>(shiftResource);
        var updatedShift = await _shiftRepository.Put(shift);
        return _mapper.Map<ShiftResource>(updatedShift);
    }

    public async Task<List<ShiftResource>> CreateShiftCutsAsync(List<ShiftResource> shiftResources, CancellationToken cancellationToken = default)
    {
        var createdShifts = new List<Shift>();
        foreach (var shiftResource in shiftResources)
        {
            var shift = _mapper.Map<Shift>(shiftResource);
            await _shiftRepository.Add(shift);
            createdShifts.Add(shift);
        }
        return _mapper.Map<List<ShiftResource>>(createdShifts);
    }

    public async Task<List<ShiftResource>> UpdateShiftCutsAsync(List<ShiftResource> shiftResources, CancellationToken cancellationToken = default)
    {
        var updatedShifts = new List<Shift>();
        foreach (var shiftResource in shiftResources)
        {
            var shift = _mapper.Map<Shift>(shiftResource);
            var updatedShift = await _shiftRepository.Put(shift);
            updatedShifts.Add(updatedShift);
        }
        return _mapper.Map<List<ShiftResource>>(updatedShifts);
    }
}