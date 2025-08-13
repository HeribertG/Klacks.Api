using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services;

public class CalendarSelectionApplicationService
{
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CalendarSelectionApplicationService> _logger;

    public CalendarSelectionApplicationService(
        ICalendarSelectionRepository calendarSelectionRepository,
        IMapper mapper,
        ILogger<CalendarSelectionApplicationService> logger)
    {
        _calendarSelectionRepository = calendarSelectionRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<CalendarSelectionResource?> GetCalendarSelectionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var calendarSelection = await _calendarSelectionRepository.Get(id);
        return calendarSelection != null ? _mapper.Map<CalendarSelectionResource>(calendarSelection) : null;
    }

    public async Task<CalendarSelectionResource?> GetCalendarSelectionWithSelectedCalendarsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var calendarSelection = await _calendarSelectionRepository.GetWithSelectedCalendars(id);
        return calendarSelection != null ? _mapper.Map<CalendarSelectionResource>(calendarSelection) : null;
    }

    public async Task<List<CalendarSelectionResource>> GetAllCalendarSelectionsAsync(CancellationToken cancellationToken = default)
    {
        var calendarSelections = await _calendarSelectionRepository.List();
        return _mapper.Map<List<CalendarSelectionResource>>(calendarSelections);
    }

    public async Task<CalendarSelectionResource> CreateCalendarSelectionAsync(CalendarSelectionResource calendarSelectionResource, CancellationToken cancellationToken = default)
    {
        var calendarSelection = _mapper.Map<CalendarSelection>(calendarSelectionResource);
        await _calendarSelectionRepository.Add(calendarSelection);
        return _mapper.Map<CalendarSelectionResource>(calendarSelection);
    }

    public async Task<CalendarSelectionResource> UpdateCalendarSelectionAsync(CalendarSelectionResource calendarSelectionResource, CancellationToken cancellationToken = default)
    {
        var calendarSelection = _mapper.Map<CalendarSelection>(calendarSelectionResource);
        var updatedCalendarSelection = await _calendarSelectionRepository.Put(calendarSelection);
        return _mapper.Map<CalendarSelectionResource>(updatedCalendarSelection);
    }

    public async Task UpdateCalendarSelectionDirectAsync(CalendarSelectionResource calendarSelectionResource, CancellationToken cancellationToken = default)
    {
        var calendarSelection = _mapper.Map<CalendarSelection>(calendarSelectionResource);
        await _calendarSelectionRepository.Update(calendarSelection);
    }

    public async Task DeleteCalendarSelectionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _calendarSelectionRepository.Delete(id);
    }
}