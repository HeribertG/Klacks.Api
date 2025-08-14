using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.CalendarSelections;

public class PostCommandHandler : IRequestHandler<PostCommand<CalendarSelectionResource>, CalendarSelectionResource?>
{
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        ICalendarSelectionRepository calendarSelectionRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _calendarSelectionRepository = calendarSelectionRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CalendarSelectionResource?> Handle(PostCommand<CalendarSelectionResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var calendarSelection = _mapper.Map<CalendarSelection>(request.Resource);
            await _calendarSelectionRepository.Add(calendarSelection);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<CalendarSelectionResource>(calendarSelection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding a new CalendarSelection. ID: {Id}", request.Resource.Id);
            throw;
        }
    }
}
