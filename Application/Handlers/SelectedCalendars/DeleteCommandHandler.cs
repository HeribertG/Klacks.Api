using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.SelectedCalendars;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<SelectedCalendarResource>, SelectedCalendarResource?>
{
    private readonly ISelectedCalendarRepository _selectedCalendarRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        ISelectedCalendarRepository selectedCalendarRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _selectedCalendarRepository = selectedCalendarRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<SelectedCalendarResource?> Handle(DeleteCommand<SelectedCalendarResource> request, CancellationToken cancellationToken)
    {
        var existingSelectedCalendar = await _selectedCalendarRepository.Get(request.Id);
        if (existingSelectedCalendar == null)
        {
            return null;
        }

        var selectedCalendarResource = _mapper.Map<SelectedCalendarResource>(existingSelectedCalendar);
        await _selectedCalendarRepository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();
        return selectedCalendarResource;
    }
}
