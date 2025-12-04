using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.SelectedCalendars;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<SelectedCalendarResource>, SelectedCalendarResource?>
{
    private readonly ISelectedCalendarRepository _selectedCalendarRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        ISelectedCalendarRepository selectedCalendarRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _selectedCalendarRepository = selectedCalendarRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<SelectedCalendarResource?> Handle(PutCommand<SelectedCalendarResource> request, CancellationToken cancellationToken)
    {
        var existingSelectedCalendar = await _selectedCalendarRepository.Get(request.Resource.Id);
        if (existingSelectedCalendar == null)
        {
            return null;
        }

        _mapper.Map(request.Resource, existingSelectedCalendar);
        await _selectedCalendarRepository.Put(existingSelectedCalendar);
        await _unitOfWork.CompleteAsync();
        return _mapper.Map<SelectedCalendarResource>(existingSelectedCalendar);
    }
}
