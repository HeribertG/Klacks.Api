using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services;

public class SelectedCalendarApplicationService
{
    private readonly ISelectedCalendarRepository _selectedCalendarRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SelectedCalendarApplicationService> _logger;

    public SelectedCalendarApplicationService(
        ISelectedCalendarRepository selectedCalendarRepository,
        IMapper mapper,
        ILogger<SelectedCalendarApplicationService> logger)
    {
        _selectedCalendarRepository = selectedCalendarRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<SelectedCalendarResource?> GetSelectedCalendarByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var selectedCalendar = await _selectedCalendarRepository.Get(id);
        return selectedCalendar != null ? _mapper.Map<SelectedCalendarResource>(selectedCalendar) : null;
    }

    public async Task<List<SelectedCalendarResource>> GetAllSelectedCalendarsAsync(CancellationToken cancellationToken = default)
    {
        var selectedCalendars = await _selectedCalendarRepository.List();
        return _mapper.Map<List<SelectedCalendarResource>>(selectedCalendars);
    }

    public async Task<SelectedCalendarResource> CreateSelectedCalendarAsync(SelectedCalendarResource selectedCalendarResource, CancellationToken cancellationToken = default)
    {
        var selectedCalendar = _mapper.Map<SelectedCalendar>(selectedCalendarResource);
        await _selectedCalendarRepository.Add(selectedCalendar);
        return _mapper.Map<SelectedCalendarResource>(selectedCalendar);
    }

    public async Task<SelectedCalendarResource> UpdateSelectedCalendarAsync(SelectedCalendarResource selectedCalendarResource, CancellationToken cancellationToken = default)
    {
        var selectedCalendar = _mapper.Map<SelectedCalendar>(selectedCalendarResource);
        var updatedSelectedCalendar = await _selectedCalendarRepository.Put(selectedCalendar);
        return _mapper.Map<SelectedCalendarResource>(updatedSelectedCalendar);
    }

    public async Task DeleteSelectedCalendarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _selectedCalendarRepository.Delete(id);
    }
}