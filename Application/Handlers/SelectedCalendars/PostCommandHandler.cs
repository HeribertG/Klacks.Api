using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.SelectedCalendars;

public class PostCommandHandler : IRequestHandler<PostCommand<SelectedCalendarResource>, SelectedCalendarResource?>
{
    private readonly ISelectedCalendarRepository _selectedCalendarRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        ISelectedCalendarRepository selectedCalendarRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _selectedCalendarRepository = selectedCalendarRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<SelectedCalendarResource?> Handle(PostCommand<SelectedCalendarResource> request, CancellationToken cancellationToken)
    {
        var selectedCalendar = _mapper.Map<SelectedCalendar>(request.Resource);
        await _selectedCalendarRepository.Add(selectedCalendar);
        await _unitOfWork.CompleteAsync();
        return _mapper.Map<SelectedCalendarResource>(selectedCalendar);
    }
}
